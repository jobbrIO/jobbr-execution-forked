using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jobbr.ComponentModel.Execution.Model;
using Jobbr.Server.ForkedExecution.Core;
using Jobbr.Server.ForkedExecution.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class JobExecutorTests
    {
        private readonly FakeGeneratedJobRunsStore jobRunFakeTuples;
        private readonly JobRunProgressUpdateStore storedProgressUpdates;
        private readonly JobRunInfoServiceMock jobRunInformationService;

        public JobExecutorTests()
        {
            this.jobRunFakeTuples = new FakeGeneratedJobRunsStore();
            this.storedProgressUpdates = new JobRunProgressUpdateStore();
            this.jobRunInformationService = new JobRunInfoServiceMock(this.jobRunFakeTuples);
        }

        private static ForkedExecutionConfiguration GivenAMinimalConfiguration()
        {
            var forkedExecutionConfiguration = new ForkedExecutionConfiguration()
            {
                BackendAddress = "notNeeded",
                JobRunDirectory = Path.GetTempPath(),
                JobRunnerExeResolver = () => "Jobbr.Server.ForkedExecution.TestEcho.exe",

            };
            return forkedExecutionConfiguration;
        }

        [TestMethod]
        public void StartEmpty_GetsNewPlanWithOneJob_ExecutesJob()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            var executor = this.GivenAStartedExecutor(forkedExecutionConfiguration);

            // Act
            var fakeJobRun = this.jobRunFakeTuples.CreateFakeJobRun();
            executor.OnPlanChanged(new List<PlannedJobRun>(new [] { fakeJobRun.PlannedJobRun, }));

            // Wait
            this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeJobRun.UniqueId].Count == 2, 3000);

            // Test
            var allStatesForJob = this.storedProgressUpdates.AllStatusUpdates[fakeJobRun.UniqueId];

            Assert.AreEqual(2, allStatesForJob.Count, "There should be two transitions instead of a timeout");
            Assert.AreEqual(JobRunStates.Started, allStatesForJob[1], "The last state should be 'Started' even if the process has exited sucesfully because the runtime needs to set the 'Complete'-State");
        }

        private ForkedJobExecutor GivenAStartedExecutor(ForkedExecutionConfiguration forkedExecutionConfiguration)
        {
            var executor = new ForkedJobExecutor(this.jobRunInformationService, forkedExecutionConfiguration, this.storedProgressUpdates);

            executor.Start();

            return executor;
        }

        [TestMethod]
        public void StartEmpty_GetsMultipleJobs_ExecutesMultipleJobs()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            var executor = this.GivenAStartedExecutor(forkedExecutionConfiguration);

            // Act: create jobruns & send plan
            var fakeJobRun1 = this.jobRunFakeTuples.CreateFakeJobRun();
            var fakeJobRun2 = this.jobRunFakeTuples.CreateFakeJobRun();
            var fakeJobRun3 = this.jobRunFakeTuples.CreateFakeJobRun();

            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun1.PlannedJobRun, fakeJobRun2.PlannedJobRun, fakeJobRun3.PlannedJobRun }));

            // Wait
            this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates.SelectMany(kvp => kvp.Value).Count() == 6, 3000);

            // Test
            var allStatesForJob1 = this.storedProgressUpdates.AllStatusUpdates[fakeJobRun1.UniqueId];
            var allStatesForJob2 = this.storedProgressUpdates.AllStatusUpdates[fakeJobRun2.UniqueId];
            var allStatesForJob3 = this.storedProgressUpdates.AllStatusUpdates[fakeJobRun3.UniqueId];

            Assert.AreEqual(2, allStatesForJob1.Count, "There should be two transitions instead of a timeout for job1");
            Assert.AreEqual(2, allStatesForJob2.Count, "There should be two transitions instead of a timeout for job2");
            Assert.AreEqual(2, allStatesForJob3.Count, "There should be two transitions instead of a timeout for job3");

            Assert.AreEqual(JobRunStates.Started, allStatesForJob1[1], "The last state of job1 should be 'Started'");
            Assert.AreEqual(JobRunStates.Started, allStatesForJob2[1], "The last state of job2 should be 'Started'");
            Assert.AreEqual(JobRunStates.Started, allStatesForJob3[1], "The last state of job3 should be 'Started'");
        }

        [TestMethod]
        public void ContainsAItemInPlan_GetsAdditional_ExecutesMultipleJobs()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            var executor = this.GivenAStartedExecutor(forkedExecutionConfiguration);

            // Act: Create & send First Plan
            var fakeJobRun1 = this.jobRunFakeTuples.CreateFakeJobRun();
            var fakeJobRun2 = this.jobRunFakeTuples.CreateFakeJobRun();

            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun1.PlannedJobRun }));
            
            // Act: Second Plan
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun2.PlannedJobRun }));

            // Wait
            var didStart2Jobs = this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates.SelectMany(kvp => kvp.Value).Count() == 2, 3000);

            // Test
            Assert.IsTrue(didStart2Jobs, "There should be two jobs that have been started");
        }

        [TestMethod]
        public void ContainsAItemInPlan_DifferentList_ExecutesOnlySecondJob()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            forkedExecutionConfiguration.MaxConcurrentJobs = 1;
            var executor = this.GivenAStartedExecutor(forkedExecutionConfiguration);

            // Act: Create & send first plan
            var fakeJobRun1 = this.jobRunFakeTuples.CreateFakeJobRun();
            var fakeJobRun2 = this.jobRunFakeTuples.CreateFakeJobRun();

            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun1.PlannedJobRun }));

            // Act: Send second plan
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun2.PlannedJobRun }));

            // Wait
            var didStart2Jobs = this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates.SelectMany(kvp => kvp.Value).Count() == 2, 3000);

            // Test
            Assert.IsTrue(didStart2Jobs, "There should be two jobs that have been started");
        }

        [TestMethod]
        public void LimitTo2Jobs_GetsNewPlanWith3Jobs_ExecutesMaxConfiguredJobs()
        {
            // Setup
            var forkedExecutionConfiguration = GivenAMinimalConfiguration();
            
            // Only run 2 jobs at a time
            forkedExecutionConfiguration.MaxConcurrentJobs = 2;
            var executor = this.GivenAStartedExecutor(forkedExecutionConfiguration);

            // Act 1: Create & Send only first plan
            var fakeJobRun1 = this.jobRunFakeTuples.CreateFakeJobRun();
            var fakeJobRun2 = this.jobRunFakeTuples.CreateFakeJobRun();
            var fakeJobRun3 = this.jobRunFakeTuples.CreateFakeJobRun();

            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun1.PlannedJobRun, fakeJobRun2.PlannedJobRun, fakeJobRun3.PlannedJobRun }));

            // Act 2: Send updated plan
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun1.PlannedJobRun, fakeJobRun2.PlannedJobRun, fakeJobRun3.PlannedJobRun }));

            // Wait
            this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeJobRun1.UniqueId].Count + allUpdates[fakeJobRun2.UniqueId].Count == 4, 3000);

            // Test
            Assert.IsFalse(this.storedProgressUpdates.AllStatusUpdates.ContainsKey(fakeJobRun3.UniqueId), "There should be no updates for the third job");
        }
    }
}