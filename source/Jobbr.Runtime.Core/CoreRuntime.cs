using System;
using Jobbr.Runtime.Core.Activation;
using Jobbr.Runtime.Core.Execution;
using Jobbr.Runtime.Logging;

namespace Jobbr.Runtime.Core
{
    public class CoreRuntime
    {
        private static readonly ILog Logger = LogProvider.For<CoreRuntime>();

        private readonly JobActivator jobActivator;

        /// <summary>
        /// Raised immediately after start and indicates that the Runtime is setting up itself
        /// </summary>
        public event EventHandler Initializing;

        /// <summary>
        /// Raised before the runtime activates the job class
        /// </summary>
        public event EventHandler Activating;

        /// <summary>
        /// Raised before the wrapper for the Run() Method is generated
        /// </summary>
        public event EventHandler WiringMethod;

        /// <summary>
        /// Raised before the Run()-Method is executed
        /// </summary>
        public event EventHandler Starting;

        /// <summary>
        /// Raised after the job has come to the end, independent of its success
        /// </summary>
        public event EventHandler<ExecutionEndedEventArgs> Ended;

        /// <summary>
        /// Raised for exceptions in the Core infrastructure that have not been handled
        /// </summary>
        public event EventHandler<InfrastructureExceptionEventArgs> InfrastructureException;

        public CoreRuntime(RuntimeConfiguration runtimeConfiguration)
        {
            var jobTypeResolver = new JobTypeResolver(runtimeConfiguration.JobTypeSearchAssembly);
            var serviceProvider = runtimeConfiguration.ServiceProvider ?? new DefaultServiceProvider();

            this.jobActivator = new JobActivator(jobTypeResolver, serviceProvider);
        }

        public void Execute(ExecutionMetadata executionMetadata)
        {
            var wasSuccessful = false;
            Exception lastException = null;

            try
            {
                this.OnInitializing();

                var jobTypeName = executionMetadata.JobType;

                var userContext = new UserContext()
                {
                    UserId = executionMetadata.UserId,
                    UserDisplayName = executionMetadata.UserDisplayName
                };

                // Register userContext as RuntimeContext in the DI if available
                Logger.Debug($"Trying to register additional dependencies if supported.");
                
                #pragma warning disable 618
                var runtimeContext = new RuntimeContext
                {
                    UserId = userContext.UserId,
                    UserDisplayName = userContext.UserDisplayName
                };
                #pragma warning restore 618

                this.jobActivator.AddDependencies(runtimeContext);

                // Create instance
                Logger.Debug($"Create instance of job based on the typename '{jobTypeName}'");
                this.OnActivating();

                var jobClassInstance = this.jobActivator.CreateInstance(jobTypeName);

                if (jobClassInstance == null)
                {
                    Logger.Error($"Cannot create activate the job based on the typename {jobTypeName}");
                    return;
                }

                // Create task as wrapper for calling the Run() Method
                Logger.Debug($"Create task as wrapper for calling the Run() Method");
                this.OnWiringMethod();

                var runWrapperFactory = new RunWrapperFactory(jobClassInstance.GetType(), executionMetadata.JobParameter, executionMetadata.InstanceParameter);
                var wrapper = runWrapperFactory.CreateWrapper(jobClassInstance, userContext);

                if (wrapper == null)
                {
                    Logger.Error("Unable to create a wrapper for the job");
                    return;
                }

                // Start 
                Logger.Debug("Starting Task to execute the Run()-Method.");
                this.OnStarting();

                wrapper.Start();

                // Wait for completion
                wasSuccessful = wrapper.WaitForCompletion();
                lastException = wrapper.Exception;
            }
            catch (Exception e)
            {
                lastException = e;

                Logger.FatalException("Exception in the Jobbr-Runtime. Please see details: ", e);
                this.OnInfrastructureException(new InfrastructureExceptionEventArgs { Exception = e });
            }
            finally
            {
                this.OnEnded(new ExecutionEndedEventArgs() { Succeeded = wasSuccessful, Exception = lastException});
            }
        }

        #region Event Invocators

        protected virtual void OnInitializing()
        {
            try
            {
                this.Initializing?.Invoke(this, EventArgs.Empty);

            }
            catch (Exception exception)
            {
                Logger.ErrorException($"Recipient of the event {nameof(this.OnInitializing)} threw an execption", exception);
            }
        }

        protected virtual void OnActivating()
        {
            try
            {
                this.Activating?.Invoke(this, EventArgs.Empty);

            }
            catch (Exception exception)
            {
                Logger.ErrorException($"Recipient of the event {nameof(this.OnActivating)} threw an execption", exception);
            }
        }

        protected virtual void OnWiringMethod()
        {
            try
            {
                this.WiringMethod?.Invoke(this, EventArgs.Empty);

            }
            catch(Exception exception)
            {
                Logger.ErrorException($"Recipient of the event {nameof(this.OnWiringMethod)} threw an execption", exception);
            }
        }

        protected virtual void OnStarting()
        {
            try
            {
                this.Starting?.Invoke(this, EventArgs.Empty);

            }
            catch (Exception exception)
            {
                Logger.ErrorException($"Recipient of the event {nameof(this.OnStarting)} threw an execption", exception);
            }
        }

        protected virtual void OnEnded(ExecutionEndedEventArgs e)
        {
            try
            {
                this.Ended?.Invoke(this, e);

            }
            catch (Exception exception)
            {
                Logger.ErrorException($"Recipient of the event {nameof(this.OnEnded)} threw an execption", exception);
            }
        }

        protected virtual void OnInfrastructureException(InfrastructureExceptionEventArgs e)
        {
            try
            {
                this.InfrastructureException?.Invoke(this, e);

            }
            catch (Exception exception)
            {
                Logger.ErrorException($"Recipient of the event {nameof(this.OnInfrastructureException)} threw an execption", exception);
            }
        }

        #endregion
    }
}