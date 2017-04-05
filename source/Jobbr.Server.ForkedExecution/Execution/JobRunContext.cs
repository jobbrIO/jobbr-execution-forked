using System;
using System.Diagnostics;
using System.IO;
using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Execution.Model;
using Jobbr.Server.ForkedExecution.Execution.ServiceMessaging;
using Jobbr.Server.ForkedExecution.Logging;

namespace Jobbr.Server.ForkedExecution.Execution
{
    /// <summary>
    /// The runner process.
    /// </summary>
    internal class JobRunContext : IJobRunContext
    {
        private static readonly ILog Logger = LogProvider.For<JobRunContext>();

        private readonly JobRunInfo jobRunInfo;

        private readonly ForkedExecutionConfiguration configuration;

        private readonly IJobRunProgressChannel progressChannel;

        private readonly ServiceMessageParser serviceMessageParser;

        private bool didReportProgress;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobRunContext"/> class.
        /// </summary>
        public JobRunContext(JobRunInfo jobRunInfoIfo, ForkedExecutionConfiguration configuration, IJobRunProgressChannel progressChannel)
        {
            this.jobRunInfo = jobRunInfoIfo;
            this.configuration = configuration;
            this.progressChannel = progressChannel;

            this.serviceMessageParser = new ServiceMessageParser();
        }

        public event EventHandler<JobRunEndedEventArgs> Ended;

        public void Start()
        {
            Logger.Info($"[{this.jobRunInfo.Id}] A new JobRunContext is starting for JobRun with id: '{this.jobRunInfo.Id}'. (JobId: '{this.jobRunInfo.JobId}', TriggerId: '{this.jobRunInfo.TriggerId}'");

            try
            {
                var workDir = this.SetupDirectories(this.jobRunInfo);

                this.StartProcess(this.jobRunInfo, workDir);

                this.progressChannel.PublishStatusUpdate(this.jobRunInfo.Id, JobRunStates.Started);
            }
            catch (Exception e)
            {
                Logger.ErrorException($"[{this.jobRunInfo.Id}] Exception thrown while starting JobRun with id: '{this.jobRunInfo.Id}'. (JobId: '{this.jobRunInfo.JobId}', TriggerId: '{this.jobRunInfo.TriggerId}'", e);
            }
        }

        protected virtual void OnEnded(JobRunEndedEventArgs e)
        {
            Logger.Info($"[{this.jobRunInfo.Id}] The Runner with ProcessId '{e.ProcInfo.Id}' has ended at '{e.ProcInfo.ExitTime}'. ExitCode: '{e.ProcInfo.ExitCode}'");

            this.Ended?.Invoke(this, e);
        }

        private void StartProcess(JobRunInfo jobRun, string workDir)
        {
            var runnerFileExe = Path.GetFullPath(this.configuration.JobRunnerExecutable);
            Logger.Info($"[{jobRun.Id}] Preparing to start the runner from '{runnerFileExe}' in '{workDir}'");

            var proc = new Process { EnableRaisingEvents = true, StartInfo = { FileName = runnerFileExe } };

            var arguments = $"--jobRunId {jobRun.Id} --server {this.configuration.BackendAddress}";

            if (this.configuration.IsRuntimeWaitingForDebugger)
            {
                arguments += " --debug";
            }

            if (this.configuration.AddJobRunnerArguments != null)
            {
                var model = new JobRunStartInfo
                {
                    JobType = jobRun.Type,
                    UniqueName = jobRun.UniqueName,
                    JobRunId = jobRun.Id,
                    JobId = jobRun.JobId,
                    TriggerId = jobRun.TriggerId,
                    UserId = jobRun.UserId
                };

                var additionalArguments = this.configuration.AddJobRunnerArguments(model);

                foreach (var additionalArgument in additionalArguments)
                {
                    if (additionalArgument.Value.Contains(" "))
                    {
                        arguments += $" --{additionalArgument.Key} \"{additionalArgument.Value}\"";
                    }
                    else
                    {
                        arguments += $" --{additionalArgument.Key} {additionalArgument.Value}";
                    }
                }
            }

            proc.StartInfo.Arguments = arguments;
            proc.StartInfo.WorkingDirectory = workDir;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;

            // Wire events
            proc.OutputDataReceived += this.ProcOnOutputDataReceived;
            proc.Exited += (o, args) => this.OnEnded(new JobRunEndedEventArgs() { ExitCode = proc.ExitCode, JobRun = jobRun, ProcInfo = proc, DidReportProgress = this.didReportProgress });

            this.progressChannel.PublishStatusUpdate(this.jobRunInfo.Id, JobRunStates.Starting);
            Logger.Info($"[{jobRun.Id}] Starting '{runnerFileExe} {arguments}' in '{workDir}'");
            proc.Start();

            Logger.Info($"[{jobRun.Id}] Started Runner with ProcessId '{proc.Id}' at '{proc.StartTime}'");
            this.progressChannel.PublishPid(jobRun.Id, proc.Id, Environment.MachineName);

            proc.BeginOutputReadLine();
        }

        private string SetupDirectories(JobRunInfo jobRun)
        {
            // Create the WorkingDir and TempDir for the execution
            var jobRunPath = Path.Combine(this.configuration.JobRunDirectory, "jobbr-" + jobRun.Id);

            Logger.Info($"[{jobRun.Id}] Preparing filesytem directories in '{this.configuration.JobRunDirectory}'");

            var tempDir = Path.Combine(jobRunPath, "temp");
            var workDir = Path.Combine(jobRunPath, "work");

            Directory.CreateDirectory(tempDir);
            Logger.Info($"[{jobRun.Id}] Created Temp-Directory '{tempDir}'");

            Directory.CreateDirectory(workDir);
            Logger.Info($"[{jobRun.Id}] Created Working-Directory '{workDir}'");

            return workDir;
        }

        private void ProcOnOutputDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            var data = dataReceivedEventArgs.Data;

            if (data == null)
            {
                return;
            }

            var lines = data.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // Detect ServiceMessage
                if (line.StartsWith("##jobbr"))
                {
                    var message = this.serviceMessageParser.Parse(line);

                    try
                    {
                        if (message != null)
                        {
                            if (this.HandleMessage(message as dynamic))
                            {
                                Logger.Debug($"[{this.jobRunInfo.Id}] Handled service-message of type '{message.GetType().Name}'. RawValue: '{line}'");
                            }
                            else
                            {
                                Logger.Warn($"[{this.jobRunInfo.Id}] Cannot handle messages of type '{message.GetType().Name}'. RawValue: '{line}'");
                            }
                        }
                        else
                        {
                            Logger.Warn($"[{this.jobRunInfo.Id}] Parsing Error while processing service message '{line}'");
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorException($"[{this.jobRunInfo.Id}] Exception while processing service message '{line}'", e);
                    }
                }
            }
        }

        private bool HandleMessage(ProgressServiceMessage message)
        {
            this.progressChannel.PublishProgressUpdate(this.jobRunInfo.Id, message.Percent);
            this.didReportProgress = true;

            return true;
        }
    }
}