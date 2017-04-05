using System.Collections.Generic;
using System.Linq;
using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Execution.Model;
using Jobbr.Server.ForkedExecution.Execution;

namespace Jobbr.Server.ForkedExecution.Tests.Infrastructure
{
    internal class JobRunContextMockFactory : IJobRunContextFactory
    {
        private readonly IJobRunProgressChannel progressChannel;
        private readonly List<MockedJobContext> contexts = new List<MockedJobContext>();

        public JobRunContextMockFactory(IJobRunProgressChannel progressChannel)
        {
            this.progressChannel = progressChannel;
        }

        public MockedJobContext this[long jobRunId] => this.contexts.Single(c => c.JobRunId == jobRunId);

        public long Count => this.contexts.Count;

        public IJobRunContext CreateJobRunContext(JobRunInfo jobRunInfo)
        {
            var mockedJobContext = new MockedJobContext(jobRunInfo, this.progressChannel);

            this.contexts.Add(mockedJobContext);

            return mockedJobContext;
        }
    }
}