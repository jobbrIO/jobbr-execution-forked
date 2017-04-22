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

            this.runWrapperFactory = new RunWrapperFactory();
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

                var task = this.runWrapperFactory.CreateRunTask(jobClassInstance, type, this.jobInfo);
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