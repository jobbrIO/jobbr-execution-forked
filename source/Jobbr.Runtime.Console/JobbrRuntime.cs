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

        private readonly Assembly defaultAssembly;
        private readonly IJobbrDependencyResolver dependencyResolver;

        public JobbrRuntime(Assembly defaultAssembly, IJobbrDependencyResolver dependencyResolver)
        {
            this.defaultAssembly = defaultAssembly;
            this.dependencyResolver = dependencyResolver;
        }

        public JobbrRuntime(Assembly defaultAssembly) : this(defaultAssembly, new NoDependencyResolver())
        {
        }

        public void Run(string[] args)
        {
            var oldRuntime = new CoreRuntime(this.defaultAssembly, this.dependencyResolver);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            Logger.Info($"JobbrRuntime started at {DateTime.UtcNow} (UTC) with cmd-arguments {String.Join(" ", args)}");

            var cmdlineOptions = ParseArguments(args);

            Logger.Info($"JobRunId:  {cmdlineOptions.JobRunId}");
            Logger.Info($"JobServer: {cmdlineOptions.JobServer}");
            Logger.Info($"IsDebug:   {cmdlineOptions.IsDebug}");

            WaitForDebugger(cmdlineOptions.IsDebug);

            var jobbrRuntimeClient = InitializeClient(cmdlineOptions);
            var jobRunInfoDto = jobbrRuntimeClient.GetJobRunInfo();

            oldRuntime.client = jobbrRuntimeClient;
            oldRuntime.RunCore(jobRunInfoDto);
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
