using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Execution.Model;

namespace Jobbr.Server.ForkedExecution.Execution
{
    public interface IJobRunContextFactory
    {
        IJobRunContext CreateJobRunContext(JobRunInfo jobRunInfo);
    }

    public class JobRunContextFactory : IJobRunContextFactory
    {
        private readonly ForkedExecutionConfiguration configuration;
        private readonly IJobRunProgressChannel progressChannel;

        public JobRunContextFactory(ForkedExecutionConfiguration configuration, IJobRunProgressChannel progressChannel)
        {
            this.configuration = configuration;
            this.progressChannel = progressChannel;
        }

        public IJobRunContext CreateJobRunContext(JobRunInfo jobRunInfo)
        {
            return new JobRunContext(jobRunInfo, this.configuration, this.progressChannel);
        }
    }
}