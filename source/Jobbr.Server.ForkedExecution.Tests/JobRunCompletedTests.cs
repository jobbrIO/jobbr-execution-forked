using System;
using System.Collections.Generic;
using Jobbr.ComponentModel.Execution.Model;
using Jobbr.Server.ForkedExecution.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class JobRunCompletedTests : TestBase
    {
        [TestMethod]
        public void ProcessExits_NonZeroExitCode_StateIsFailed()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            var executor = this.GivenAMockedExecutor(forkedExecutionConfiguration);

            // Act
            var fakeJobRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);

            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun.PlannedJobRun, }));
            
            this.jobRunContextMockFactory[fakeJobRun.Id].RaiseEnded(-3683737);

            // Test
            var allStatesForJob = this.ProgressChannelStore.AllStatusUpdates[fakeJobRun.Id];

            Assert.AreEqual(2, allStatesForJob.Count, "There should be two transitions instead of a timeout");
            Assert.AreEqual(JobRunStates.Failed, allStatesForJob[1], "The Mock should have issued the state 'Starting'");
        }

        [TestMethod]
        public void ProcessExists_WithZeroExitCode_StateIsCompleted()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            var executor = this.GivenAMockedExecutor(forkedExecutionConfiguration);

            // Act
            var fakeJobRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);

            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun.PlannedJobRun, }));

            this.jobRunContextMockFactory[fakeJobRun.Id].RaiseEnded(0);

            // Test
            var allStatesForJob = this.ProgressChannelStore.AllStatusUpdates[fakeJobRun.Id];

            Assert.AreEqual(2, allStatesForJob.Count, "There should be two transitions instead of a timeout");
            Assert.AreEqual(JobRunStates.Completed, allStatesForJob[1], "The Mock should have issued the state 'Starting'");

        }

        [TestMethod]
        public void ProcessExists_HadProgressWithZeroExitCode_PercentIs100()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            var executor = this.GivenAMockedExecutor(forkedExecutionConfiguration);

            // Act
            var fakeJobRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);

            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun.PlannedJobRun, }));

            this.jobRunContextMockFactory[fakeJobRun.Id].RaiseProgressUpdate(54);
            this.jobRunContextMockFactory[fakeJobRun.Id].RaiseEnded(0);

            // Test
            Assert.AreEqual(2, this.ProgressChannelStore.AllProgressUpdates[fakeJobRun.Id].Count, "There should be the explicit progress update and the one at the end.");
            Assert.AreEqual(100, this.ProgressChannelStore.AllProgressUpdates[fakeJobRun.Id][1], "There should be a 100% message if the job has ended without error code");
        }

        [TestMethod]
        public void ProcessExists_HadProgressWithNonZeroExitCode_PercentIsUntouched()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            var executor = this.GivenAMockedExecutor(forkedExecutionConfiguration);

            // Act
            var fakeJobRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);

            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun.PlannedJobRun, }));

            this.jobRunContextMockFactory[fakeJobRun.Id].RaiseProgressUpdate(54);
            this.jobRunContextMockFactory[fakeJobRun.Id].RaiseEnded(-2564);

            // Test
            Assert.AreEqual(1, this.ProgressChannelStore.AllProgressUpdates[fakeJobRun.Id].Count, "There should be only one progress change");
            Assert.AreEqual(54, this.ProgressChannelStore.AllProgressUpdates[fakeJobRun.Id][0], "There should be a 100% message if the job has ended without error code");
        }

        [TestMethod]
        public void ProcessExists_NoProgressWithZeroExitCode_NoProgress()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            var executor = this.GivenAMockedExecutor(forkedExecutionConfiguration);

            // Act
            var fakeJobRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);

            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun.PlannedJobRun, }));

            this.jobRunContextMockFactory[fakeJobRun.Id].RaiseEnded(0);

            // Test
            Assert.IsFalse(this.ProgressChannelStore.AllProgressUpdates.ContainsKey(fakeJobRun.Id), "Not expecting any progress updates for this job");
        }

        [TestMethod]
        public void ProcessExists_NoProgressWithNonZeroExitCode_NoProgress()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            var executor = this.GivenAMockedExecutor(forkedExecutionConfiguration);

            // Act
            var fakeJobRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);

            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun.PlannedJobRun, }));

            this.jobRunContextMockFactory[fakeJobRun.Id].RaiseEnded(-2564);

            // Test
            Assert.IsFalse(this.ProgressChannelStore.AllProgressUpdates.ContainsKey(fakeJobRun.Id), "Not expecting any progress updates for this job");
        }
    }
}
