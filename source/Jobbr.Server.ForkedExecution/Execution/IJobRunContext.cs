using System;

namespace Jobbr.Server.ForkedExecution.Execution
{
    public interface IJobRunContext
    {
        event EventHandler<JobRunEndedEventArgs> Ended;

        void Start();
    }
}