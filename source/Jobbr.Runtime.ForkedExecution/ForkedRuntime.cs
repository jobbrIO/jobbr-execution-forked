using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Jobbr.Runtime.ForkedExecution.RestClient;
using Microsoft.Extensions.Logging;

namespace Jobbr.Runtime.ForkedExecution
{
    public class ForkedRuntime
    {
        private readonly ILogger<ForkedRuntime> _logger;
        private readonly CoreRuntime _coreRuntime;
        private ForkedExecutionRestClient _forkedExecutionRestClient;

        public ForkedRuntime(ILoggerFactory loggerFactory, RuntimeConfiguration runtimeConfiguration)
        {
            if (runtimeConfiguration == null)
            {
                throw new ArgumentNullException(nameof(runtimeConfiguration));
            }

            _logger = loggerFactory.CreateLogger<ForkedRuntime>();

            _coreRuntime = new CoreRuntime(runtimeConfiguration);

            // Wire Events to publish status
            _coreRuntime.Initializing += (sender, args) => _forkedExecutionRestClient.PublishState(JobRunState.Initializing);
            _coreRuntime.Starting += (sender, args) => _forkedExecutionRestClient.PublishState(JobRunState.Processing);

            _coreRuntime.Ended += CoreRuntimeOnEnded;
        }

        public void Run(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            
            _logger.LogInformation("ForkedRuntime started at {now} (UTC) with cmd-arguments {arguments}", DateTime.UtcNow, string.Join(" ", args));

            var cmdlineOptions = ParseArguments(args);

            _logger.LogInformation("JobRunId: {jobRunId}", cmdlineOptions.JobRunId);
            _logger.LogInformation("JobServer: {jobServer}", cmdlineOptions.JobServer); 
            _logger.LogInformation("IsDebug: {isDebug}", cmdlineOptions.IsDebug);

            WaitForDebugger(cmdlineOptions.IsDebug);

            // Create client
            var jobbrRuntimeClient = new ForkedExecutionRestClient(cmdlineOptions.JobServer, cmdlineOptions.JobRunId);
            this._forkedExecutionRestClient = jobbrRuntimeClient;

            jobbrRuntimeClient.PublishState(JobRunState.Connected);

            var jobRunInfoDto = this._forkedExecutionRestClient.GetJobRunInfo();

            var jobRunInfo = new ExecutionMetadata
            {
                JobType = jobRunInfoDto.JobType,
                JobParameter = jobRunInfoDto.JobParameter,
                InstanceParameter = jobRunInfoDto.InstanceParameter,
                UserId = jobRunInfoDto.UserId,
                UserDisplayName = jobRunInfoDto.UserDisplayName
            };

            _coreRuntime.Execute(jobRunInfo);
        }

        private static CommandlineOptions ParseArguments(IEnumerable<string> args)
        {
            return Parser.Default.ParseArguments<CommandlineOptions>(args).Value;
        }

        private static void WaitForDebugger(bool isDebugEnabled)
        {
            if (isDebugEnabled)
            {
                var beginWaitForDebugger = DateTime.Now;
                var endWaitForDebugger = beginWaitForDebugger.AddSeconds(10);
                var pressedEnter = false;

                Console.WriteLine(string.Empty);
                Console.WriteLine(">>> DEBUG-Mode is enabled. You have 10s to attach a Debugger");
                Console.Write("    or press enter to continue. Counting...");

                new TaskFactory().StartNew(
                    () =>
                    {
                        Console.ReadLine();
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

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            _logger.LogError((Exception)unhandledExceptionEventArgs.ExceptionObject, "Unhandled Infrastructure Exception in Jobbr-Runtime. Please contact the developers!");
        }

        private void CoreRuntimeOnEnded(object sender, ExecutionEndedEventArgs executionEndedEventArgs)
        {
            if (!executionEndedEventArgs.Succeeded)
            {
                // Indicate failure also via exit code
                Environment.ExitCode = 1;
            }

            _forkedExecutionRestClient.PublishState(JobRunState.Finishing);

            // Are there any files to collect?
            var allFiles = Directory.GetFiles(Directory.GetCurrentDirectory());

            if (allFiles.Any())
            {
                _forkedExecutionRestClient.PublishState(JobRunState.Collecting);
                _forkedExecutionRestClient.SendFiles(allFiles);
            }

            if (executionEndedEventArgs.Succeeded)
            {
                _forkedExecutionRestClient.PublishState(JobRunState.Completed);
            }
            else
            {
                _forkedExecutionRestClient.PublishState(JobRunState.Failed);
            }
        }

    }
}
