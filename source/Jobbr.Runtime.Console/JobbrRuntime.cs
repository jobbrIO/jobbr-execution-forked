using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Jobbr.ConsoleApp.Runtime.Logging;
using Jobbr.Runtime.Console.RestClient;
using Jobbr.Runtime.Core;

namespace Jobbr.Runtime.Console
{
    public class JobbrRuntime
    {
        private static readonly ILog Logger = LogProvider.For<JobbrRuntime>();

        private CoreRuntime coreRuntime;
        private ForkedExecutionRestClient forkedExecutionRestClient;

        public JobbrRuntime(Assembly defaultAssembly, IJobActivator dependencyResolver)
        {
            this.coreRuntime = new CoreRuntime(new RuntimeConfiguration(defaultAssembly, dependencyResolver));

            this.coreRuntime.StateChanged += this.CoreRuntimeOnOnStateChanged;
            this.coreRuntime.Finishing += this.CoreRuntimeOnFinishing;
        }

        public JobbrRuntime(Assembly defaultAssembly) : this(defaultAssembly, new DefaultActivator())
        {
        }

        public void Run(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            Logger.Info($"JobbrRuntime started at {DateTime.UtcNow} (UTC) with cmd-arguments {string.Join(" ", args)}");

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

            var jobRunInfo = new JobRunInfo
            {
                JobType = jobRunInfoDto.JobType,
                JobParameter = jobRunInfoDto.JobParameter,
                InstanceParameter = jobRunInfoDto.InstanceParameter,
                UserId = jobRunInfoDto.UserId,
                UserDisplayName = jobRunInfoDto.UserDisplayName
            };

            this.coreRuntime.RunCore(jobRunInfo);
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

        private static CommandlineOptions ParseArguments(string[] args)
        {
            var commandlineOptions = new CommandlineOptions();
            Parser.Default.ParseArguments(args, commandlineOptions);
            var cmdlineOptions = commandlineOptions;
            return cmdlineOptions;
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            Logger.FatalException("Unhandled Infrastructure Exception in Jobbr-Runtime. Please contact the developers!", (Exception)unhandledExceptionEventArgs.ExceptionObject);
        }

        private void CoreRuntimeOnFinishing(object sender, FinishingEventArgs finishingEventArgs)
        {
            this.forkedExecutionRestClient.PublishState(JobRunState.Collecting);

            var allFiles = Directory.GetFiles(Directory.GetCurrentDirectory());

            this.forkedExecutionRestClient.SendFiles(allFiles);
        }

        private void CoreRuntimeOnOnStateChanged(object sender, StateChangedEventArgs stateChangedEventArgs)
        {
            this.forkedExecutionRestClient.PublishState(stateChangedEventArgs.State);
        }
    }
}
