using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Jobbr.ConsoleApp.Runtime.Logging;
using Newtonsoft.Json;

namespace Jobbr.Runtime.Console
{
    public class JobbrRuntime
    {
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
            var oldRuntime = new OldJobbrRuntime(this.defaultAssembly, this.dependencyResolver);

            oldRuntime.Run(args);
        }
    }

    public class OldJobbrRuntime : IDisposable
    {
        private static readonly ILog Logger = LogProvider.For<OldJobbrRuntime>();
        
        private readonly Assembly defaultAssembly;

        private readonly IJobbrDependencyResolver dependencyResolver;

        private JobbrRuntimeClient client;

        private CommandlineOptions commandlineOptions;

        private object jobInstance;

        private CancellationTokenSource cancellationTokenSource;

        private Task jobRunTask;

        private JobRunInfoDto jobInfo;

        private RuntimeContext context;

        public OldJobbrRuntime(Assembly defaultAssembly, IJobbrDependencyResolver dependencyResolver)
        {
            this.defaultAssembly = defaultAssembly;
            this.dependencyResolver = dependencyResolver;
        }

        public OldJobbrRuntime(Assembly defaultAssembly) : this(defaultAssembly, new NoDependencyResolver())
        {
        }

        public void Run(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            Logger.Info($"JobbrRuntime started at {DateTime.UtcNow} (UTC) with cmd-arguments {string.Join(" ", args)}");

            this.ParseArguments(args);

            Logger.Info($"JobRunId:  {this.commandlineOptions.JobRunId}");
            Logger.Info($"JobServer: {this.commandlineOptions.JobServer}");
            Logger.Info($"IsDebug:   {this.commandlineOptions.IsDebug}");

            this.InitializeClient();
            
            try
            {
                this.WaitForDebuggerIfEnabled();

                this.InitializeJob();

                this.StartJob();

                this.WaitForCompletion();

                this.Collect();
            }
            catch (Exception e)
            {
                Logger.FatalException("Exception in the Jobbr-Runtime. Please see details: ", e);
                Environment.ExitCode = 1;

                try
                {
                    this.Collect();
                }
                catch (Exception)
                {
                }
            }

            this.End();
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            Logger.FatalException("Unhandled Infrastructure Exception in Jobbr-Runtime. Please contact the developers!", (Exception)unhandledExceptionEventArgs.ExceptionObject);
        }

        private void WaitForCompletion()
        {
            if (this.jobRunTask == null)
            {
                return;
            }

            try
            {
                this.jobRunTask.Wait(this.cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                Logger.ErrorException("Exception while waiting for completion of job", e);
            }

            if (this.jobRunTask.IsFaulted)
            {
                Logger.ErrorException("The execution of the job has faulted. See Exception for details.", this.jobRunTask.Exception);
                this.client.PublishState(JobRunState.Failed);
            }
            else
            {
                this.client.PublishState(JobRunState.Finishing);
            }
        }

        private void End()
        {
            if (this.jobRunTask == null || this.jobRunTask.IsFaulted)
            {
                this.client.PublishState(JobRunState.Failed);
                Environment.ExitCode = 1;
            }
            else
            {
                this.client.PublishState(JobRunState.Completed);
            }
        }

        private void Collect()
        {
            if (this.jobRunTask != null)
            {
                this.client.PublishState(JobRunState.Collecting);

                var allFiles = Directory.GetFiles(Directory.GetCurrentDirectory());

                this.client.SendFiles(allFiles);
            }
        }

        private void StartJob()
        {
            if (this.jobInstance == null)
            {
                return;
            }

            var runMethods = this.jobInstance.GetType().GetMethods().Where(m => string.Equals(m.Name, "Run", StringComparison.Ordinal) && m.IsPublic).ToList();

            this.cancellationTokenSource = new CancellationTokenSource();

            if (runMethods.Any())
            {
                // Try to use the method with 2 concrete parameters
                var parameterizedMethod = runMethods.FirstOrDefault(m => m.GetParameters().Length == 2);
                if (parameterizedMethod != null)
                {
                    var jobParamValue = this.jobInfo.JobParameter ?? "<null>";
                    var instanceParamValue = this.jobInfo.InstanceParameter ?? "<null>";

                    var jobParamJsonString = jobParamValue.ToString();
                    var instanceParamJsonString = instanceParamValue.ToString();

                    // Note: We cannot use string interpolation here, because LibLog is using string.format again and will fail if there are { } chars in the string, even if there is no formatting needed.
                    Logger.DebugFormat($"Decided to use parameterized method '{parameterizedMethod}' with JobParameter '{0}' and InstanceParameters '{1}'.", jobParamJsonString, instanceParamJsonString);
                    var allParams = parameterizedMethod.GetParameters().OrderBy(p => p.Position).ToList();

                    var param1Type = allParams[0].ParameterType;
                    var param2Type = allParams[1].ParameterType;

                    var param1Name = allParams[0].Name;
                    var param2Name = allParams[1].Name;

                    // Casting in the most preferrable type
                    var jobParameterValue = this.GetCastedParameterValue(param1Name, param1Type, "job", this.jobInfo.JobParameter);
                    var instanceParamaterValue = this.GetCastedParameterValue(param2Name, param2Type, "instance", this.jobInfo.InstanceParameter);

                    Logger.Debug("Initializing task for JobRun");
                    this.jobRunTask = new Task(() => { parameterizedMethod.Invoke(this.jobInstance, new[] { jobParameterValue, instanceParamaterValue }); }, this.cancellationTokenSource.Token);
                }
                else
                {
                    var fallBackMethod = runMethods.FirstOrDefault(m => !m.GetParameters().Any());

                    if (fallBackMethod != null)
                    {
                        Logger.Debug($"Decided to use parameterless method '{fallBackMethod}'");
                        this.jobRunTask = new Task(() => fallBackMethod.Invoke(this.jobInstance, null), this.cancellationTokenSource.Token);
                    }
                }

                if (this.jobRunTask != null)
                {
                    Logger.Debug("Starting Task to execute the Run()-Method.");

                    this.jobRunTask.Start();
                    this.client.PublishState(JobRunState.Processing);
                }
                else
                {
                    Logger.Error("None of your Run()-Methods are compatible with Jobbr. Please see coeumentation");
                }
            }
            else
            {
                Logger.Error("Unable to find an entrypoint to call your job. Is there at least a public Run()-Method?");
                this.client.PublishState(JobRunState.Failed);
            }
        }

        private object GetCastedParameterValue(string parameterName, Type targetType, string jobbrParamName, object value)
        {
            object castedValue;

            Logger.Info($"Casting {jobbrParamName}-parameter to its target value '{targetType}' based on the Run()-Parameter {parameterName}");

            // Try to cast them to specific types
            if (value == null)
            {
                Logger.Debug($"The {jobbrParamName}-parameter is null - no cast needed.");
                castedValue = null;
            }
            else if (targetType == typeof(object))
            {
                Logger.Debug($"The {jobbrParamName}-parameter is of type 'object' - no cast needed.");
                castedValue = value;
            }
            else
            {
                Logger.Debug(string.Format("The {0}-parameter '{1}' is from type '{2}'. Casting this value to '{2}'", jobbrParamName, parameterName, targetType));
                castedValue = JsonConvert.DeserializeObject(value.ToString(), targetType);
            }

            return castedValue;
        }

        private void InitializeJob()
        {
            this.client.PublishState(JobRunState.Initializing);
            this.jobInfo = this.client.GetJobRunInfo();
            this.SetRuntimeContext();

            var typeName = this.jobInfo.JobType;

            Logger.Debug($"Trying to resolve the specified type '{this.jobInfo.JobType}'...");
            
            var type = this.ResolveType(typeName);

            if (type == null)
            {
                Logger.Error($"Unable to resolve the type '{this.jobInfo.JobType}'!");

                this.client.PublishState(JobRunState.Failed);
            }
            else
            {
                Logger.Info($"Type '{this.jobInfo.JobType}' has been resolved to '{type}'. Activating now.");

                try
                {
                    this.jobInstance = this.dependencyResolver.GetService(type);
                }
                catch (Exception exception)
                {
                    Logger.ErrorException("Failed while activating type '{0}'. See Exception for details!", exception, type);
                }
            }
        }

        private void SetRuntimeContext()
        {
            this.context = new RuntimeContext
            {
                UserId = this.jobInfo.UserId,
                UserDisplayName = this.jobInfo.UserDisplayName
            };

            var registrator = this.dependencyResolver as IJobbrDependencyRegistrator;

            registrator?.RegisterInstance(this.context);
        }

        private Type ResolveType(string typeName)
        {
            Logger.Debug($"Resolve type using '{typeName}' like a full qualified CLR-Name");
            var type = Type.GetType(typeName);

            if (type == null && this.defaultAssembly != null)
            {
                Logger.Debug($"Trying to resolve '{typeName}' by the assembly '{this.defaultAssembly.FullName}'");
                type = this.defaultAssembly.GetType(typeName);
            }

            if (type == null)
            {
                // Search in all Assemblies
                var allReferenced = Assembly.GetExecutingAssembly().GetReferencedAssemblies();

                Logger.Debug($"Trying to resolve type by asking all referenced assemblies ('{string.Join(", ", allReferenced.Select(a => a.Name))}')");

                foreach (var assemblyName in allReferenced)
                {
                    var assembly = Assembly.Load(assemblyName);

                    var foundType = assembly.GetType(typeName, false, true);

                    if (foundType != null)
                    {
                        type = foundType;
                    }
                }
            }

            if (type == null)
            {
                Logger.Debug($"Still no luck finding '{typeName}' somewhere. Iterating through all types and comparing class-names. Please hold on");

                // Absolutely no clue
                var matchingTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => string.Equals(x.Name, typeName, StringComparison.Ordinal) && x.IsClass && !x.IsAbstract).ToList();

                if (matchingTypes.Count() == 1)
                {
                    Logger.Debug($"Found matching type: '{matchingTypes[0]}'");
                    type = matchingTypes.First();
                }
                else if (matchingTypes.Count > 1)
                {
                    Logger.Warn($"More than one matching type found for '{typeName}'. Matches: {string.Join(", ", matchingTypes.Select(t => t.FullName))}");
                }
                else
                {
                    Logger.Warn($"No matching type found for '{typeName}'.");
                }
            }

            return type;
        }

        private void InitializeClient()
        {
            this.client = new JobbrRuntimeClient(this.commandlineOptions.JobServer, this.commandlineOptions.JobRunId);
            this.client.PublishState(JobRunState.Connected);
        }

        private void WaitForDebuggerIfEnabled()
        {
            if (this.commandlineOptions.IsDebug)
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

        private void ParseArguments(string[] args)
        {
            this.commandlineOptions = new CommandlineOptions();
            Parser.Default.ParseArguments(args, this.commandlineOptions);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.client != null)
                {
                    this.client.Dispose();
                    this.client = null;
                }

                if (this.cancellationTokenSource != null)
                {
                    this.cancellationTokenSource.Dispose();
                    this.cancellationTokenSource = null;
                }

                if (this.jobRunTask != null)
                {
                    this.jobRunTask.Dispose();
                    this.jobRunTask = null;
                }
            }
        }
    }
}
