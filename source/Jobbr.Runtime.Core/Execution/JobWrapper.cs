using System;
using System.Threading;
using System.Threading.Tasks;
using Jobbr.Runtime.Logging;

namespace Jobbr.Runtime.Core.Execution
{
    internal class JobWrapper
    {
        private static readonly ILog Logger = LogProvider.For<JobWrapper>();

        private readonly Task task;

        internal JobWrapper(Action action)
        {
            this.task = new Task(action);
        }

        internal void Start()
        {
            this.task.Start();
        }

        internal void Wait(CancellationToken token)
        {
            this.task.Wait(token);
        }

        public Exception Exception { get; private set; }

        internal bool WaitForCompletion()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            try
            {
                this.Wait(cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                Logger.ErrorException("Exception while waiting for completion of job", e);
                this.Exception = e;
                return false;
            }

            if (this.task.IsFaulted)
            {
                this.Exception = this.task.Exception;
                Logger.ErrorException("The execution of the job has faulted. See Exception for details.", this.task.Exception);
                return false;
            }

            return true;
        }
    }
}