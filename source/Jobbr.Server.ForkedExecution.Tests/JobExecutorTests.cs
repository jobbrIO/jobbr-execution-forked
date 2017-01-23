using System.Collections.Generic;
using System.IO;
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

        [TestMethod]
        public void StartEmpty_GetsNewPlanWithOneJob_ExecutesJob()
        {
            // Setup
            var forkedExecutionConfiguration = new ForkedExecutionConfiguration()
            {
                BackendAddress = "any",
                JobRunDirectory = Path.GetTempPath(),
                JobRunnerExeResolver = () => "Jobbr.Server.ForkedExecution.TestEcho.exe"
            };

            var fakeJobRun = this.jobRunFakeTuples.CreateFakeJobRun();
            var executor = new ForkedJobExecutor(this.jobRunInformationService, forkedExecutionConfiguration, this.storedProgressUpdates);

            // Act
            executor.Start();
            executor.OnPlanChanged(new List<PlannedJobRun>(new [] { fakeJobRun.PlannedJobRun, }));

            // Wait
            this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeJobRun.UniqueId].Count == 2, 3000);

            // Test
            var allStatesForJob = this.storedProgressUpdates.AllStatusUpdates[fakeJobRun.UniqueId];

            Assert.AreEqual(2, allStatesForJob.Count, "There should be two transitions instead of a timeout");
            Assert.AreEqual(JobRunStates.Started, allStatesForJob[1], "The last state should be 'Started' even if the process has exited sucesfully because the runtime needs to set the 'Complete'-State");
        }

        [TestMethod]
        public void StartEmpty_GetsMultipleJobs_ExecutesMultipleJobs()
        {
            // Setup
            var forkedExecutionConfiguration = new ForkedExecutionConfiguration()
            {
                BackendAddress = "any",
                JobRunDirectory = Path.GetTempPath(),
                JobRunnerExeResolver = () => "Jobbr.Server.ForkedExecution.TestEcho.exe",
            };

            var fakeJobRun1 = this.jobRunFakeTuples.CreateFakeJobRun();
            var fakeJobRun2 = this.jobRunFakeTuples.CreateFakeJobRun();
            var fakeJobRun3 = this.jobRunFakeTuples.CreateFakeJobRun();

            var executor = new ForkedJobExecutor(this.jobRunInformationService, forkedExecutionConfiguration, this.storedProgressUpdates);

            // Act
            executor.Start();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun1.PlannedJobRun, fakeJobRun2.PlannedJobRun, fakeJobRun3.PlannedJobRun }));

            // Wait
            this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeJobRun1.UniqueId].Count + allUpdates[fakeJobRun2.UniqueId].Count + allUpdates[fakeJobRun3.UniqueId].Count == 6, 3000);

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
        public void LimitTo2Jobs_GetsNewPlanWith3Jobs_ExecutesMaxConfiguredJobs()
        {
            // Setup
            var forkedExecutionConfiguration = new ForkedExecutionConfiguration()
            {
                BackendAddress = "any",
                JobRunDirectory = Path.GetTempPath(),
                JobRunnerExeResolver = () => "Jobbr.Server.ForkedExecution.TestEcho.exe",

                // Only run 2 jobs at a time
                MaxConcurrentJobs = 2
            };

            var fakeJobRun1 = this.jobRunFakeTuples.CreateFakeJobRun();
            var fakeJobRun2 = this.jobRunFakeTuples.CreateFakeJobRun();
            var fakeJobRun3 = this.jobRunFakeTuples.CreateFakeJobRun();

            var executor = new ForkedJobExecutor(this.jobRunInformationService, forkedExecutionConfiguration, this.storedProgressUpdates);

            // Act
            executor.Start();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeJobRun1.PlannedJobRun, fakeJobRun2.PlannedJobRun, fakeJobRun3.PlannedJobRun }));

            // Wait
            this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeJobRun1.UniqueId].Count + allUpdates[fakeJobRun2.UniqueId].Count == 4, 3000);

            Assert.IsFalse(this.storedProgressUpdates.AllStatusUpdates.ContainsKey(fakeJobRun3.UniqueId), "There should be no updates for the third job");
        }
    }
}