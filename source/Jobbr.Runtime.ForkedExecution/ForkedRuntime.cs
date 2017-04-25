using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Jobbr.Runtime.Core;
using Jobbr.Runtime.ForkedExecution.Logging;
using Jobbr.Runtime.ForkedExecution.RestClient;

namespace Jobbr.Runtime.ForkedExecution
{
    public class ForkedRuntime
    {
        private static readonly ILog Logger = LogProvider.For<ForkedRuntime>();

        private readonly CoreRuntime coreRuntime;
        private ForkedExecutionRestClient forkedExecutionRestClient;

        public ForkedRuntime(RuntimeConfiguration runtimeConfiguration)
        {
            if (runtimeConfiguration == null)
            {
                throw new ArgumentNullException(nameof(runtimeConfiguration));
            }

            this.coreRuntime = new CoreRuntime(runtimeConfiguration);

            // Wire Events to publish status
            this.coreRuntime.Initializing += (sender, args) => this.forkedExecutionRestClient.PublishState(JobRunState.Initializing);
            this.coreRuntime.Starting += (sender, args) => this.forkedExecutionRestClient.PublishState(JobRunState.Processing);

            this.coreRuntime.Ended += this.CoreRuntimeOnEnded;
        }

        public ForkedRuntime() : this(new RuntimeConfiguration())
        {
        }

        public void Run(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            Logger.Info($"ForkedRuntime started at {DateTime.UtcNow} (UTC) with cmd-arguments {string.Join(" ", args)}");

            var cmdlineOptions = ParseArguments(args);

            Logger.Info($"JobRunId:  {cmdlineOptions.JobRunId}");
            Logger.Info($"JobServer: {cmdlineOptions.JobServer}");
            Logger.Info($"IsDebug:   {cmdlineOptions.IsDebug}");

            WaitForDebugger(cmdlineOptions.IsDebug);

            // Create client
            var jobbrRuntimeClient = new ForkedExecutionRestClient(cmdlineOptions.JobServer, cmdlineOptions.JobRunId);
            this.forkedExecutionRestClient = jobbrRuntimeClient;

            jobbrRuntimeClient.PublishState(JobRunState.Connected);

            var jobRunInfoDto = this.forkedExecutionRestClient.GetJobRunInfo();

            var jobRunInfo = new ExecutionMetadata
            {
                JobType = jobRunInfoDto.JobType,
                JobParameter = jobRunInfoDto.JobParameter,
                InstanceParameter = jobRunInfoDto.InstanceParameter,
                UserId = jobRunInfoDto.UserId,
                UserDisplayName = jobRunInfoDto.UserDisplayName
            };

            this.coreRuntime.Execute(jobRunInfo);
        }

        private static CommandlineOptions ParseArguments(string[] args)
        {
            var commandlineOptions = new CommandlineOptions();
            Parser.Default.ParseArguments(args, commandlineOptions);
            var cmdlineOptions = commandlineOptions;
            return cmdlineOptions;
        }

        private static void WaitForDebugger(bool isDebugEnabled)
        {
            if (isDebugEnabled)
            {
                var beginWaitForDebugger = DateTime.Now;
                var endWaitForDebugger = beginWaitForDebugger.AddSeconds(10);
                var pressedEnter = false;

                System.Console.WriteLine(string.Empty);
                System.Console.WriteLine(">>> DEBUG-Mode is enabled. You have 10s to attach a Debugger");
                System.Console.Write("    or press enter to continue. Counting...");

                new TaskFactory().StartNew(
                    () =>
                    {
                        System.Console.ReadLine();
                        pressedEnter = true;
                    });

                while (!(pressedEnter || Debugger.IsAttached || endWaitForDebugger < DateTime.Now))
                {
                    Thread.Sleep(500);
                }
            }

            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            Logger.FatalException("Unhandled Infrastructure Exception in Jobbr-Runtime. Please contact the developers!", (Exception)unhandledExceptionEventArgs.ExceptionObject);
        }

        private void CoreRuntimeOnEnded(object sender, ExecutionEndedEventArgs executionEndedEventArgs)
        {
            if (!executionEndedEventArgs.Succeeded)
            {
                // Indicate failure also via exit code
                Environment.ExitCode = 1;
            }

            this.forkedExecutionRestClient.PublishState(JobRunState.Finishing);

            // Are there any files to collect?
            var allFiles = Directory.GetFiles(Directory.GetCurrentDirectory());

            if (allFiles.Any())
            {
                this.forkedExecutionRestClient.PublishState(JobRunState.Collecting);
                this.forkedExecutionRestClient.SendFiles(allFiles);
            }

            if (executionEndedEventArgs.Succeeded)
            {
                this.forkedExecutionRestClient.PublishState(JobRunState.Completed);
            }
            else
            {
                this.forkedExecutionRestClient.PublishState(JobRunState.Failed);
            }
        }

    }
}
