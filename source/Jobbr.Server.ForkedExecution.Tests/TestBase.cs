using System.IO;
using Jobbr.Server.ForkedExecution.Core;
using Jobbr.Server.ForkedExecution.Tests.Infrastructure;

namespace Jobbr.Server.ForkedExecution.Tests
{
    public class TestBase
    {
        protected FakeGeneratedJobRunsStore jobRunFakeTuples;
        protected JobRunProgressUpdateStore storedProgressUpdates;
        protected JobRunInfoServiceMock jobRunInformationService;

        public TestBase()
        {
            this.jobRunFakeTuples = new FakeGeneratedJobRunsStore();
            this.storedProgressUpdates = new JobRunProgressUpdateStore();
            this.jobRunInformationService = new JobRunInfoServiceMock(this.jobRunFakeTuples);
        }

        protected static ForkedExecutionConfiguration GivenAMinimalConfiguration()
        {
            var forkedExecutionConfiguration = new ForkedExecutionConfiguration()
            {
                BackendAddress = "notNeeded",
                JobRunDirectory = Path.GetTempPath(),
                JobRunnerExeResolver = () => "Jobbr.Server.ForkedExecution.TestEcho.exe"
            };

            return forkedExecutionConfiguration;
        }

        protected ForkedJobExecutor GivenAStartedExecutor(ForkedExecutionConfiguration forkedExecutionConfiguration)
        {
            var executor = new ForkedJobExecutor(this.jobRunInformationService, this.storedProgressUpdates, forkedExecutionConfiguration);

            executor.Start();

            return executor;
        }
    }
}