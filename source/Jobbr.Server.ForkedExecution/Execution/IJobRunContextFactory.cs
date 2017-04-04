using Jobbr.ComponentModel.Execution.Model;

namespace Jobbr.Server.ForkedExecution.Execution
{
    public interface IJobRunContextFactory
    {
        IJobRunContext CreateJobRunContext(JobRunInfo jobRunInfo);
    }
}