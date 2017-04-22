using System;
using System.Threading;
using System.Threading.Tasks;
using Jobbr.Runtime.Core.Logging;

namespace Jobbr.Runtime.Core
{
    public class JobWrapper
    {
        private static readonly ILog Logger = LogProvider.For<JobWrapper>();

        private readonly Task task;

        public JobWrapper(Action action)
        {
            this.task = new Task(action);
        }

        public void Start()
        {
            this.task.Start();
        }

        public void Wait(CancellationToken token)
        {
            this.task.Wait(token);
        }

        public bool WaitForCompletion()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            try
            {
                this.Wait(cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                Logger.ErrorException("Exception while waiting for completion of job", e);
                return false;
            }

            if (this.task.IsFaulted)
            {
                Logger.ErrorException("The execution of the job has faulted. See Exception for details.", this.task.Exception);
                return false;
            }

            return true;
        }
    }
}