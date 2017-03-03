using System;
using System.Diagnostics;
using System.IO;
using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Execution.Model;
using Jobbr.Server.ForkedExecution.Core.ServiceMessaging;
using Jobbr.Server.ForkedExecution.Logging;

namespace Jobbr.Server.ForkedExecution.Core
{
    /// <summary>
    /// The runner process.
    /// </summary>
    internal class JobRunContext
    {
        private static readonly ILog Logger = LogProvider.For<JobRunContext>();

        private readonly JobRunInfo jobRunInfo;

        private readonly ForkedExecutionConfiguration configuration;

        private readonly IJobRunProgressChannel progressChannel;

        private readonly ServiceMessageParser serviceMessageParser;

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
            Logger.InfoFormat("[{0}] A new JobRunContext is starting for JobRun with id: '{1}'. (JobId: '{2}', TriggerId: '{3}'", this.jobRunInfo.Id, this.jobRunInfo.Id, this.jobRunInfo.JobId, this.jobRunInfo.TriggerId);

            try
            {
                var workDir = this.SetupDirectories(this.jobRunInfo);

                this.StartProcess(this.jobRunInfo, workDir);

                this.progressChannel.PublishStatusUpdate(this.jobRunInfo.Id, JobRunStates.Started);

                // TODO: Discuss if this information is still needed??? this.stateService.SetPidForJobRun(jobRun, proc.Id);
            }
            catch (Exception e)
            {
                Logger.ErrorException($"[{this.jobRunInfo.Id}] Exception thrown while starting JobRun with id: '{this.jobRunInfo.Id}'. (JobId: '{this.jobRunInfo.JobId}', TriggerId: '{this.jobRunInfo.TriggerId}'", e);
            }
        }

        protected virtual void OnEnded(JobRunEndedEventArgs e)
        {
            Logger.InfoFormat("[{0}] The Runner with ProcessId '{1}' has ended at '{2}'. ExitCode: '{3}'", this.jobRunInfo.Id, e.ProcInfo.Id, e.ProcInfo.ExitTime, e.ProcInfo.ExitCode);

            this.Ended?.Invoke(this, e);
        }

        private void StartProcess(JobRunInfo jobRun, string workDir)
        {
            var runnerFileExe = Path.GetFullPath(this.configuration.JobRunnerExeResolver());
            Logger.InfoFormat("[{0}] Preparing to start the runner from '{1}' in '{2}'", jobRun.Id, runnerFileExe, workDir);

            var proc = new Process { EnableRaisingEvents = true, StartInfo = { FileName = runnerFileExe } };

            var arguments = $"--jobRunId {jobRun.Id} --server {this.configuration.BackendAddress}";

            if (this.configuration.IsRuntimeWaitingForDebugger)
            {
                arguments += " --debug";
            }

            if (this.configuration.CustomJobRunnerParameters != null)
            {
                var customParameters = this.configuration.CustomJobRunnerParameters(jobRun.Type, jobRun.UniqueName);

                foreach (var customParameter in customParameters)
                {
                    if (customParameter.Value.Contains(" "))
                    {
                        arguments += $" --{customParameter.Key} \"{customParameter.Value}\"";
                    }
                    else
                    {
                        arguments += $" --{customParameter.Key} {customParameter.Value}";
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
            proc.Exited += (o, args) => this.OnEnded(new JobRunEndedEventArgs() { ExitCode = proc.ExitCode, JobRun = jobRun, ProcInfo = proc });

            this.progressChannel.PublishStatusUpdate(this.jobRunInfo.Id, JobRunStates.Starting);

            Logger.InfoFormat("[{0}] Starting '{1} {2}' in '{3}'", jobRun.Id, runnerFileExe, arguments, workDir);
            proc.Start();

            Logger.InfoFormat("[{0}] Started Runner with ProcessId '{1}' at '{2}'", jobRun.Id, proc.Id, proc.StartTime);

            proc.BeginOutputReadLine();
        }

        private string SetupDirectories(JobRunInfo jobRun)
        {
            // Create the WorkingDir and TempDir for the execution
            var jobRunPath = Path.Combine(this.configuration.JobRunDirectory, "jobbr-" + jobRun.Id);

            Logger.InfoFormat("[{0}] Preparing filesytem directories in '{1}'", jobRun.Id, this.configuration.JobRunDirectory);

            var tempDir = Path.Combine(jobRunPath, "temp");
            var workDir = Path.Combine(jobRunPath, "work");

            Directory.CreateDirectory(tempDir);
            Logger.InfoFormat("[{0}] Created Temp-Directory '{1}'", jobRun.Id, tempDir);

            Directory.CreateDirectory(workDir);
            Logger.InfoFormat("[{0}] Created Working-Directory '{1}'", jobRun.Id, workDir);

            // TODO???: Do we still need that somewhere else?: this.stateService.UpdateJobRunDirectories(this.jobRun, workDir, tempDir);
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
                                Logger.DebugFormat("[{0}] Handled service-message of type '{1}'. RawValue: '{2}'", this.jobRunInfo.Id, message.GetType().Name, line);
                            }
                            else
                            {
                                // TODO: Implement this type!
                                Logger.WarnFormat("[{0}] Cannot handle messages of type '{1}'. RawValue: '{2}'", this.jobRunInfo.Id, message.GetType().Name, line);
                            }
                        }
                        else
                        {
                            Logger.WarnFormat("[{0}] Parsing Error while processing service message '{1}'", this.jobRunInfo.Id, line);
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
            return true;
        }
    }
}