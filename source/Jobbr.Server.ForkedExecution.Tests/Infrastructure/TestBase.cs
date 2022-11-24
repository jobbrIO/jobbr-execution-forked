using System.IO;
using Jobbr.Server.ForkedExecution.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobbr.Server.ForkedExecution.Tests.Infrastructure
{
    public class TestBase
    {
        protected ProgressChannelStore ProgressChannelStore;

        protected FakeGeneratedJobRunsStore JobRunFakeTuples;
        protected JobRunInfoServiceMock JobRunInformationService;
        protected PeriodicTimerMock PeriodicTimerMock;
        protected ManualTimeProvider ManualTimeProvider;
        internal JobRunContextMockFactory JobRunContextMockFactory;

        public TestBase()
        {
            JobRunFakeTuples = new FakeGeneratedJobRunsStore();
            ProgressChannelStore = new ProgressChannelStore();
            JobRunInformationService = new JobRunInfoServiceMock(JobRunFakeTuples);
        }

        protected static ForkedExecutionConfiguration GivenAMinimalConfiguration()
        {
            var forkedExecutionConfiguration = new ForkedExecutionConfiguration
            {
                BackendAddress = "notNeeded",
                JobRunDirectory = Path.GetTempPath(),
                JobRunnerExecutable = "Jobbr.Server.ForkedExecution.TestEcho.exe",
                MaxConcurrentProcesses = 4
            };

            return forkedExecutionConfiguration;
        }

        protected ForkedJobExecutor GivenAMockedExecutor(ForkedExecutionConfiguration forkedExecutionConfiguration)
        {
            JobRunContextMockFactory = new JobRunContextMockFactory(ProgressChannelStore);

            PeriodicTimerMock = new PeriodicTimerMock();
            ManualTimeProvider = new ManualTimeProvider();

            var executor = new ForkedJobExecutor(new NullLoggerFactory(), JobRunContextMockFactory, JobRunInformationService, ProgressChannelStore, PeriodicTimerMock, ManualTimeProvider, forkedExecutionConfiguration);

            return executor;
        }

        protected ForkedJobExecutor GivenAStartedExecutor(ForkedExecutionConfiguration forkedExecutionConfiguration)
        {
            PeriodicTimerMock = new PeriodicTimerMock();
            ManualTimeProvider = new ManualTimeProvider();

            var jobRunContextFactory = new JobRunContextFactory(new NullLoggerFactory(), forkedExecutionConfiguration, ProgressChannelStore);

            var executor = new ForkedJobExecutor(new NullLoggerFactory(), jobRunContextFactory, JobRunInformationService, ProgressChannelStore, PeriodicTimerMock, ManualTimeProvider, forkedExecutionConfiguration);

            executor.Start();

            return executor;
        }
    }
}