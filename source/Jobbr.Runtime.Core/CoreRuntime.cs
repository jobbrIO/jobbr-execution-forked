using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobbr.Runtime.Core.Logging;
using Newtonsoft.Json;

namespace Jobbr.Runtime.Core
{
    public class CoreRuntime
    {
        private static readonly ILog Logger = LogProvider.For<CoreRuntime>();

        private JobRunInfo jobInfo;

        private readonly JobTypeResolver jobTypeResolver;

        private readonly IJobActivator dependencyResolver;

        private RuntimeContext context;

        public event EventHandler<StateChangedEventArgs> StateChanged;

        public event EventHandler<FinishingEventArgs> Finishing;

        public CoreRuntime(RuntimeConfiguration runtimeConfiguration)
        {
            this.jobTypeResolver = new JobTypeResolver(runtimeConfiguration.JobTypeSearchAssembly);

            this.dependencyResolver = runtimeConfiguration.JobActivator;
        }

        public void RunCore(JobRunInfo jobRunInfo)
        {
            this.jobInfo = jobRunInfo;

            var executionResult = false;

            try
            {
                this.PublishState(JobRunState.Initializing);

                var jobTypeName = this.jobInfo.JobType;

                // Resolve Type
                Logger.Debug($"Trying to resolve the specified type '{jobTypeName}'...");

                var type = this.jobTypeResolver.ResolveType(jobTypeName);
                if (type == null)
                {
                    Logger.Error($"Unable to resolve the type '{jobTypeName}'!");
                    return;
                }

                // Activate Type
                Logger.Debug($"Trying to activate the specified type '{type}'...");

                var jobClassInstance = this.CreateJobClassInstance(jobTypeName, type);
                if (jobClassInstance == null)
                {
                    Logger.Error($"Unable to create an instance ot the type '{type}'!");
                    return;
                }

                // Create task as wrapper for calling the Run() Method
                Logger.Debug($"Create task as wrapper for calling the Run() Method");

                var task = this.CreateRunTask(jobClassInstance, jobClassInstance.GetType());
                if (task == null)
                {
                    Logger.Error("Unable to create task as a wrapper for the job");
                    return;
                }

                // Start 
                Logger.Debug("Starting Task to execute the Run()-Method.");

                task.Start();
                this.PublishState(JobRunState.Processing);

                // Wait for completion
                executionResult = this.WaitForCompletion(task);
            }
            catch (Exception e)
            {
                Logger.FatalException("Exception in the Jobbr-Runtime. Please see details: ", e);
            }
            finally
            {
                this.PublishState(JobRunState.Finishing);

                this.OnFinishing(new FinishingEventArgs() { Successful = executionResult });

                if (executionResult)
                {
                    this.PublishState(JobRunState.Completed);
                }
                else
                {
                    this.PublishState(JobRunState.Failed);
                }
            }
        }

        private void PublishState(JobRunState state)
        {
            this.OnStateChanged(new StateChangedEventArgs() { State = state });
        }

        private bool WaitForCompletion(Task runTask)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            try
            {
                runTask.Wait(cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                Logger.ErrorException("Exception while waiting for completion of job", e);
                return false;
            }

            if (runTask.IsFaulted)
            {
                Logger.ErrorException("The execution of the job has faulted. See Exception for details.", runTask.Exception);
                return false;
            }

            return true;
        }

        private Task CreateRunTask(object jobClassInstance, Type jobType)
        {
            var runMethods = jobType.GetMethods().Where(m => string.Equals(m.Name, "Run", StringComparison.Ordinal) && m.IsPublic).ToList();

            if (!runMethods.Any())
            {
                Logger.Error("Unable to find an entrypoint to call your job. Is there at least a public Run()-Method?");
                return null;
            }

            Action runMethodWrapper = null;

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

                runMethodWrapper = () => { parameterizedMethod.Invoke(jobClassInstance, new[] {jobParameterValue, instanceParamaterValue}); };
            }
            else
            {
                var fallBackMethod = runMethods.FirstOrDefault(m => !m.GetParameters().Any());

                if (fallBackMethod != null)
                {
                    Logger.Debug($"Decided to use parameterless method '{fallBackMethod}'");
                    runMethodWrapper = () => fallBackMethod.Invoke(jobClassInstance, null);
                }
            }

            if (runMethodWrapper == null)
            {
                Logger.Error("None of your Run()-Methods are compatible with Jobbr. Please see documentation");
            return null;
        }

            Logger.Debug("Initializing task for JobRun");
            var cancellationTokenSource = new CancellationTokenSource();

            return new Task(runMethodWrapper, cancellationTokenSource.Token);
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

        private object CreateJobClassInstance(string jobTypeName, Type type)
        {
            Logger.Info($"Type '{jobTypeName}' has been resolved to '{type}'. Activating now.");

            try
            {
                this.SetRuntimeContext();

                return this.dependencyResolver.Activate(type);
            }
            catch (Exception exception)
            {
                Logger.ErrorException("Failed while activating type '{0}'. See Exception for details!", exception, type);
                return null;
            }
        }

        private void SetRuntimeContext()
        {
            this.context = new RuntimeContext
            {
                UserId = this.jobInfo.UserId,
                UserDisplayName = this.jobInfo.UserDisplayName
            };

            var registrator = this.dependencyResolver as IRuntimeContextRegistrator;

            registrator?.RegisterInstance(this.context);
        }

        protected virtual void OnStateChanged(StateChangedEventArgs e)
        {
            this.StateChanged?.Invoke(this, e);
        }

        protected virtual void OnFinishing(FinishingEventArgs e)
        {
            this.Finishing?.Invoke(this, e);
        }
    }
}