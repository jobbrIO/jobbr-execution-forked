using System;
using System.Threading;
using System.Threading.Tasks;
using Jobbr.Runtime.Core.Logging;

namespace Jobbr.Runtime.Core
{
    public class CoreRuntime
    {
        private static readonly ILog Logger = LogProvider.For<CoreRuntime>();

        private JobRunInfo jobInfo;

        private readonly JobTypeResolver jobTypeResolver;

        private readonly IJobActivator dependencyResolver;

        private RuntimeContext context;

        private RunWrapperFactory runWrapperFactory;

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

            var wasSuccessful = false;

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

                // Register additional dependencies in the DI if available and activate
                Logger.Info($"Type '{jobTypeName}' has been resolved to '{type}'. Activating now.");
                this.RegisterDependencies();

                Logger.Debug($"Trying to activate the specified type '{type}'...");

                var jobClassInstance = this.CreateJobClassInstance(type);
                if (jobClassInstance == null)
                {
                    Logger.Error($"Unable to create an instance ot the type '{type}'!");
                    return;
                }

                // Create task as wrapper for calling the Run() Method
                Logger.Debug($"Create task as wrapper for calling the Run() Method");
                this.runWrapperFactory = new RunWrapperFactory(jobClassInstance.GetType(), this.jobInfo.JobParameter, this.jobInfo.InstanceParameter);

                var wrapper = this.runWrapperFactory.CreateWrapper(jobClassInstance);
                if (wrapper == null)
                {
                    Logger.Error("Unable to create a wrapper for the job");
                    return;
                }

                // Start 
                Logger.Debug("Starting Task to execute the Run()-Method.");

                wrapper.Start();
                this.PublishState(JobRunState.Processing);

                // Wait for completion
                wasSuccessful = wrapper.WaitForCompletion();
            }
            catch (Exception e)
            {
                Logger.FatalException("Exception in the Jobbr-Runtime. Please see details: ", e);
            }
            finally
            {
                this.PublishState(JobRunState.Finishing);

                this.OnFinishing(new FinishingEventArgs() { Successful = wasSuccessful });

                if (wasSuccessful)
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

        private object CreateJobClassInstance(Type type)
        {
            try
            {
                return this.dependencyResolver.Activate(type);
            }
            catch (Exception exception)
            {
                Logger.ErrorException("Failed while activating type '{0}'. See Exception for details!", exception, type);
                return null;
            }
        }

        private void RegisterDependencies()
        {
            this.context = new RuntimeContext
            {
                UserId = this.jobInfo.UserId,
                UserDisplayName = this.jobInfo.UserDisplayName
            };

            var registrator = this.dependencyResolver as IRuntimeContextRegistrator;

            try
            {
                registrator?.RegisterInstance(this.context);

            }
            catch (Exception e)
            {
                Logger.WarnException($"Unable to register additional dependencies on {registrator}!", e);
            }
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