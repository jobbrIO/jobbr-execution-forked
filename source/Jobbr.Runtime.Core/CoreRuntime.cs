using System;
using Jobbr.Runtime.Core.Logging;

namespace Jobbr.Runtime.Core
{
    public class CoreRuntime
    {
        private static readonly ILog Logger = LogProvider.For<CoreRuntime>();

        private JobRunInfo jobInfo;

        private readonly JobTypeResolver jobTypeResolver;

        private readonly IServiceProvider serviceProvider;

        private RunWrapperFactory runWrapperFactory;

        public event EventHandler<StateChangedEventArgs> StateChanged;

        public event EventHandler<FinishingEventArgs> Finishing;

        public CoreRuntime(RuntimeConfiguration runtimeConfiguration)
        {
            this.jobTypeResolver = new JobTypeResolver(runtimeConfiguration.JobTypeSearchAssembly);

            this.serviceProvider = runtimeConfiguration.ServiceProvider;
        }

        public void RunCore(JobRunInfo jobRunInfo)
        {
            this.jobInfo = jobRunInfo;

            var wasSuccessful = false;

            try
            {
                this.PublishState(JobRunState.Initializing);

                var jobTypeName = this.jobInfo.JobType;

                Logger.Debug($"Trying to register additional dependencies if supported.");
                this.RegisterDependencies(new RuntimeContext
                {
                    UserId = this.jobInfo.UserId,
                    UserDisplayName = this.jobInfo.UserDisplayName
                });

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
                return this.serviceProvider.GetService(type);
            }
            catch (Exception exception)
            {
                Logger.ErrorException("Failed while activating type '{0}'. See Exception for details!", exception, type);
                return null;
            }
        }

        private void RegisterDependencies(params object[] additionalDependencies)
        {
            var registrator = this.serviceProvider as IConfigurableServiceProvider;

            try
            {
                foreach (var dep in additionalDependencies)
                {
                    registrator?.RegisterInstance(dep);
                }

            }
            catch (Exception e)
            {
                Logger.WarnException($"Unable to register additional dependencies on {registrator}!", e);
            }
        }

        protected virtual void OnStateChanged(StateChangedEventArgs e)
        {
            try
            {
                this.StateChanged?.Invoke(this, e);

            }
            catch (Exception exception)
            {
                Logger.ErrorException($"Recipient of the event {nameof(this.OnStateChanged)} threw an execption", exception);
            }
        }

        protected virtual void OnFinishing(FinishingEventArgs e)
        {
            try
            {
                this.Finishing?.Invoke(this, e);

            }
            catch (Exception exception)
            {
                Logger.ErrorException($"Recipient of the event {nameof(this.OnFinishing)} threw an execption", exception);
            }
        }
    }
}