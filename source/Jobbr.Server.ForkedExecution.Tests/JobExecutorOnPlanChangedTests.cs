using System;
using System.Collections.Generic;
using System.Linq;
using Jobbr.ComponentModel.Execution.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class JobExecutorOnPlanChangedTests : TestBase
    {
        [TestMethod]
        public void StartEmpty_GetsNewPlanWithOneJob_ExecutesJob()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            var executor = this.GivenAStartedExecutor(forkedExecutionConfiguration);

            // Act
            var fakeJobRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            executor.OnPlanChanged(new List<PlannedJobRun>(new [] { fakeJobRun.PlannedJobRun, }));

            // Wait
            this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeJobRun.Id].Count == 2, 3000);

            // Test
            var allStatesForJob = this.storedProgressUpdates.AllStatusUpdates[fakeJobRun.Id];

            Assert.AreEqual(2, allStatesForJob.Count, "There should be two transitions instead of a timeout");
            Assert.AreEqual(JobRunStates.Started, allStatesForJob[1], "The last state should be 'Started' even if the process has exited sucesfully because the runtime needs to set the 'Complete'-State");
        }

        [TestMethod]
        public void StartEmpty_GetsMultipleJobs_ExecutesMultipleJobs()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            var executor = this.GivenAStartedExecutor(forkedExecutionConfiguration);

            // Act: create jobruns & send plan
            var fakeJobRun1 = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            var fakeJobRun2 = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            var fakeJobRun3 = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);

            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun1.PlannedJobRun, fakeJobRun2.PlannedJobRun, fakeJobRun3.PlannedJobRun }));

            // Wait for 3 Jobs with at least 2 updates each
            this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates.Count == 3 && allUpdates.All(kvp => kvp.Value.Count >= 2), 3000);

            // Test
            var allStatesForJob1 = this.storedProgressUpdates.AllStatusUpdates[fakeJobRun1.Id];
            var allStatesForJob2 = this.storedProgressUpdates.AllStatusUpdates[fakeJobRun2.Id];
            var allStatesForJob3 = this.storedProgressUpdates.AllStatusUpdates[fakeJobRun3.Id];

            Assert.IsTrue(allStatesForJob1.Count >= 2, "There should be at least two updates after 3s for job1, but only got: " + string.Join(", ", allStatesForJob1.Select(s => s)));
            Assert.IsTrue(allStatesForJob2.Count >= 2, "There should be at least two updates after 3s for job2, but only got: " + string.Join(", ", allStatesForJob2.Select(s => s)));
            Assert.IsTrue(allStatesForJob3.Count >= 2, "There should be at least two updates after 3s for job3, but only got: " + string.Join(", ", allStatesForJob3.Select(s => s)));

            Assert.AreEqual(JobRunStates.Started, allStatesForJob1[1], "The second state of job1 should be 'Started'");
            Assert.AreEqual(JobRunStates.Started, allStatesForJob2[1], "The second state of job2 should be 'Started'");
            Assert.AreEqual(JobRunStates.Started, allStatesForJob3[1], "The second state of job3 should be 'Started'");
        }

        [TestMethod]
        public void ContainsAItemInPlan_GetsAdditional_ExecutesMultipleJobs()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            var executor = this.GivenAStartedExecutor(forkedExecutionConfiguration);

            // Act: Create & send First Plan
            var fakeJobRun1 = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            var fakeJobRun2 = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);

            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun1.PlannedJobRun }));
            
            // Act: Second Plan that also contains the first item (would not start the first anymore...)
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun1.PlannedJobRun, fakeJobRun2.PlannedJobRun }));

            // Wait
            var didStart2Jobs = this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates.Count == 2 && allUpdates.All(kvp => kvp.Value.Count >= 2), 5000);

            // Test
        }

        [TestMethod]
        public void ContainsAnItemInPlan_DifferentList_ExecutesOnlySecondJob()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            forkedExecutionConfiguration.MaxConcurrentProcesses = 1;
            var executor = this.GivenAStartedExecutor(forkedExecutionConfiguration);

            // Act: Create & send first plan
            var fakeJobRun1 = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            var fakeJobRun2 = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);

            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun1.PlannedJobRun }));

            // Act: Send second plan
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun2.PlannedJobRun }));

            // Wait
            var didStart2Jobs = this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates.SelectMany(kvp => kvp.Value).Count(p => p == JobRunStates.Started) == 2, 3000);

            // Test
            Assert.IsFalse(didStart2Jobs, "Only the second job run should have been started");
            var statesPerJobRun = string.Join("\n", this.storedProgressUpdates.AllStatusUpdates.Select(u => $"- JobRun #{u.Key}, States: {string.Join(",", u.Value)}"));
            Assert.IsTrue(didStart2Jobs, "There should be two jobs that have been started after 3s instead the following states:\n\n" + statesPerJobRun);
        }

        [TestMethod]
        public void StartdateInFuture_GetsChangedToNow_ShouldExecute()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            forkedExecutionConfiguration.MaxConcurrentProcesses = 1;
            var executor = this.GivenAStartedExecutor(forkedExecutionConfiguration);

            // Act: Create & send first plan
            var fakeJobRun1 = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow.AddDays(1));

            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun1.PlannedJobRun }));

            // Act: Send second plan
            var updatedJobRun1 = new PlannedJobRun { Id = fakeJobRun1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow };
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { updatedJobRun1 }));

            // Wait
            var didStartJob = this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates.SelectMany(kvp => kvp.Value).Count() == 2, 3000);

            // Test
            Assert.IsTrue(didStartJob, "A job should have been started.");
        }

        [TestMethod]
        public void LimitTo2Jobs_GetsNewPlanWith3Jobs_ExecutesMaxConfiguredJobs()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            
            // Only run 2 jobs at a time
            forkedExecutionConfiguration.MaxConcurrentProcesses = 2;
            var executor = this.GivenAStartedExecutor(forkedExecutionConfiguration);

            // Act 1: Create & Send only first plan
            var fakeJobRun1 = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            var fakeJobRun2 = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            var fakeJobRun3 = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);

            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun1.PlannedJobRun, fakeJobRun2.PlannedJobRun, fakeJobRun3.PlannedJobRun }));

            // Act 2: Send updated plan
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun1.PlannedJobRun, fakeJobRun2.PlannedJobRun, fakeJobRun3.PlannedJobRun }));

            // Wait
            this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeJobRun1.Id].Count + allUpdates[fakeJobRun2.Id].Count == 4, 3000);

            // Test
            Assert.IsFalse(this.storedProgressUpdates.AllStatusUpdates.ContainsKey(fakeJobRun3.Id), "There should be no updates for the third job");
        }
    }
}