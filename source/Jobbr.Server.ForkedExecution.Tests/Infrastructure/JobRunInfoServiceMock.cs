using System;
using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Execution.Model;

namespace Jobbr.Server.ForkedExecution.Tests.Infrastructure
{
    /// <summary>
    ///  Wraps the InMemory FakeGeneration store to be able to work as a IJobRunInformationService
    /// </summary>
    public class JobRunInfoServiceMock : IJobRunInformationService
    {
        private readonly FakeGeneratedJobRunsStore fakeStore;

        public JobRunInfoServiceMock(FakeGeneratedJobRunsStore fakeStore)
        {
            this.fakeStore = fakeStore;
        }

        public JobRunInfo GetByUniqueId(Guid uniqueId)
        {
            return this.fakeStore.GetByUniqueUid(uniqueId)?.JobRunInfo;
        }

        public JobRunInfo GetByJobRunId(long jobRunId)
        {
            return this.fakeStore.GetByJobRunId(jobRunId)?.JobRunInfo;
        }
    }
}