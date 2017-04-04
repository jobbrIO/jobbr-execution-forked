using System.IO;
using Jobbr.Server.ForkedExecution.Execution;

namespace Jobbr.Server.ForkedExecution.Tests.Infrastructure
{
    public class TestBase
    {
        protected FakeGeneratedJobRunsStore jobRunFakeTuples;
        protected JobRunProgressUpdateStore storedProgressUpdates;
        protected JobRunInfoServiceMock jobRunInformationService;
        private PeriodicTimerMock periodicTimerMock;

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
                JobRunnerExecutable = "Jobbr.Server.ForkedExecution.TestEcho.exe",
                MaxConcurrentProcesses = 4
            };

            return forkedExecutionConfiguration;
        }

        protected ForkedJobExecutor GivenAStartedExecutor(ForkedExecutionConfiguration forkedExecutionConfiguration)
        {
            this.periodicTimerMock = new PeriodicTimerMock();

            var executor = new ForkedJobExecutor(this.jobRunInformationService, this.storedProgressUpdates, this.periodicTimerMock,  forkedExecutionConfiguration);

            executor.Start();

            return executor;
        }
    }
}