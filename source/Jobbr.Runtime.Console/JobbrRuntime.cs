using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Jobbr.ConsoleApp.Runtime.Logging;

namespace Jobbr.Runtime.Console
{
    public class JobbrRuntime
    {
        private static readonly ILog Logger = LogProvider.For<JobbrRuntime>();

        private CoreRuntime coreRuntime;
        private JobbrRuntimeClient jobbrRuntimeClient;

        public JobbrRuntime(Assembly defaultAssembly, IJobbrDependencyResolver dependencyResolver)
        {
            this.coreRuntime = new CoreRuntime(defaultAssembly, dependencyResolver);

            this.coreRuntime.StateChanged += this.CoreRuntimeOnOnStateChanged;
        }

        private void CoreRuntimeOnOnStateChanged(object sender, StateChangedEventArgs stateChangedEventArgs)
        {
            this.jobbrRuntimeClient.PublishState(stateChangedEventArgs.State);
        }

        public JobbrRuntime(Assembly defaultAssembly) : this(defaultAssembly, new NoDependencyResolver())
        {
        }

        public void Run(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            Logger.Info($"JobbrRuntime started at {DateTime.UtcNow} (UTC) with cmd-arguments {String.Join(" ", args)}");

            var cmdlineOptions = ParseArguments(args);

            Logger.Info($"JobRunId:  {cmdlineOptions.JobRunId}");
            Logger.Info($"JobServer: {cmdlineOptions.JobServer}");
            Logger.Info($"IsDebug:   {cmdlineOptions.IsDebug}");

            WaitForDebugger(cmdlineOptions.IsDebug);

            this.jobbrRuntimeClient = InitializeClient(cmdlineOptions);
            var jobRunInfoDto = this.jobbrRuntimeClient.GetJobRunInfo();

            this.coreRuntime.client = this.jobbrRuntimeClient;
            this.coreRuntime.RunCore(jobRunInfoDto);
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

        private static JobbrRuntimeClient InitializeClient(CommandlineOptions cmdlineOptions)
        {
            var jobbrRuntimeClient = new JobbrRuntimeClient(cmdlineOptions.JobServer, cmdlineOptions.JobRunId);
            jobbrRuntimeClient.PublishState(JobRunState.Connected);
            return jobbrRuntimeClient;
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
    }
}
