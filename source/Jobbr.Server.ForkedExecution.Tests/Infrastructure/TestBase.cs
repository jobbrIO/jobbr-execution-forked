using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Execution.Model;
using Jobbr.Server.ForkedExecution.Execution;

namespace Jobbr.Server.ForkedExecution.Tests.Infrastructure
{
    public class TestBase
    {
        protected FakeGeneratedJobRunsStore jobRunFakeTuples;
        protected JobRunProgressUpdateStore storedProgressUpdates;
        protected JobRunInfoServiceMock jobRunInformationService;
        protected PeriodicTimerMock periodicTimerMock;
        protected ManualTimeProvider manualTimeProvider;
        internal JobRunContextMockFactory jobRunContextMockFactory;

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

        protected ForkedJobExecutor GivenAMockedExecutor(ForkedExecutionConfiguration forkedExecutionConfiguration)
        {
            this.jobRunContextMockFactory = new JobRunContextMockFactory(this.storedProgressUpdates);

            this.periodicTimerMock = new PeriodicTimerMock();
            this.manualTimeProvider = new ManualTimeProvider();

            var executor = new ForkedJobExecutor(this.jobRunContextMockFactory, this.jobRunInformationService, this.storedProgressUpdates, this.periodicTimerMock, this.manualTimeProvider, forkedExecutionConfiguration);

            return executor;
        }

        protected ForkedJobExecutor GivenAStartedExecutor(ForkedExecutionConfiguration forkedExecutionConfiguration)
        {
            this.periodicTimerMock = new PeriodicTimerMock();
            this.manualTimeProvider = new ManualTimeProvider();

            var jobRunContextFactory = new JobRunContextFactory(forkedExecutionConfiguration, this.storedProgressUpdates);

            var executor = new ForkedJobExecutor(jobRunContextFactory,  this.jobRunInformationService, this.storedProgressUpdates, this.periodicTimerMock, this.manualTimeProvider, forkedExecutionConfiguration);

            executor.Start();

            return executor;
        }
    }

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


    public class MockedJobContext : IJobRunContext
    {
        private readonly JobRunInfo jobRunInfo;
        private readonly IJobRunProgressChannel progressChannel;

        public MockedJobContext(JobRunInfo jobRunInfo, IJobRunProgressChannel progressChannel)
        {
            this.jobRunInfo = jobRunInfo;
            this.progressChannel = progressChannel;
        }

        public event EventHandler<JobRunEndedEventArgs> Ended;

        public long JobRunId => this.jobRunInfo.Id;

        public void Start()
        {
        }

        public void RaiseProgressUpdate(double progress)
        {
            this.progressChannel.PublishProgressUpdate(this.jobRunInfo.Id, progress);
        }

        public void RaiseStatusChange(JobRunStates state)
        {
            this.progressChannel.PublishStatusUpdate(this.jobRunInfo.Id, state);
        }

        public void RaiseEnded(int exitCode = 0)
        {
            this.OnEnded(new JobRunEndedEventArgs() { ExitCode = exitCode, JobRun = this.jobRunInfo });
        }

        protected virtual void OnEnded(JobRunEndedEventArgs e)
        {
            this.Ended?.Invoke(this, e);
        }
    }

}