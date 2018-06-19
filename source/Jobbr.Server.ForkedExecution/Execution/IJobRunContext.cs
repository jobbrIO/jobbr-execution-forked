using System;

namespace Jobbr.Server.ForkedExecution.Execution
{
    public interface IJobRunContext
    {
        long JobRunId { get; }

        event EventHandler<JobRunEndedEventArgs> Ended;

        void Start();
    }
}