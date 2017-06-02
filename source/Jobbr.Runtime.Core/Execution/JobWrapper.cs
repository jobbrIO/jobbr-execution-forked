using System;
using System.Security.Principal;
using System.Threading;
using Jobbr.Runtime.Logging;

namespace Jobbr.Runtime.Core.Execution
{
    internal class JobWrapper
    {
        private static readonly ILog Logger = LogProvider.For<JobWrapper>();

        private readonly Thread thread;

        internal JobWrapper(Action action, UserContext runtimeContext)
        {
            this.thread = new Thread(() =>
            {
                var previousPrincipal = Thread.CurrentPrincipal;

                try
                {
                    if (!string.IsNullOrWhiteSpace(runtimeContext.UserId))
                    {
                        Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(runtimeContext.UserId, "JobbrIdentity"), new string[0]);
                    }

                    action();
                }
                catch (Exception e)
                {
                    this.Exception = e;
                }
                finally
                {
                    Thread.CurrentPrincipal = previousPrincipal;
                }
            });
        }

        internal void Start()
        {
            this.thread.Start();
        }

        public Exception Exception { get; private set; }

        internal bool WaitForCompletion()
        {
            try
            {
                this.thread.Join();
            }
            catch (Exception e)
            {
                Logger.ErrorException("Exception while waiting for completion of job", e);
                this.Exception = e;
                return false;
            }

            if (this.Exception != null)
            {
                Logger.ErrorException("The execution of the job has faulted. See Exception for details.", this.Exception);
                return false;
            }

            return true;
        }
    }
}