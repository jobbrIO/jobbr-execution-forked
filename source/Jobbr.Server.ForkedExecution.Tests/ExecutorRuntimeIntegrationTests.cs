using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Jobbr.ComponentModel.Execution.Model;
using Jobbr.Server.ForkedExecution.BackChannel;
using Jobbr.Server.ForkedExecution.TestRunner.TestJobs;
using Jobbr.Server.ForkedExecution.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class ExecutorRuntimeIntegrationTests : TestBase
    {
        private void GivenAStartedBackChannelHost(ForkedExecutionConfiguration config)
        {
            config.BackendAddress = string.Empty;

            new ConfigurationValidator().Validate(config);

            var backChannelHost = new BackChannelWebHost(new JobbrServiceProviderMock(this.jobRunInformationService, this.ProgressChannelStore), config);
            backChannelHost.Start();
        }

        [TestMethod]
        public void Startup_ExecutorAndBackChannel_BothRunning()
        {
            var config = GivenAMinimalConfiguration();
            this.GivenAStartedBackChannelHost(config);
            this.GivenAStartedExecutor(config);

            var statusResponse = new HttpClient().GetAsync(config.BackendAddress + "/fex/jobrun/524868244").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, statusResponse.StatusCode);
        }

        [TestMethod]
        public void PlannedJob_AfterStart_WillCallback()
        {
            // Setup
            var config = GivenAMinimalConfiguration();
            config.JobRunnerExecutable = "Jobbr.Server.ForkedExecution.TestRunner.exe";

            this.GivenAStartedBackChannelHost(config);
            var executor = this.GivenAStartedExecutor(config);

            var fakeRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);

            // Act
            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new [] { fakeRun.PlannedJobRun }));

            // Test
            var hasConnected = this.ProgressChannelStore.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Connected), 3000);

            Assert.IsTrue(hasConnected, "The runner executable should connect within 1s");

            // Tearddown: Wait for Failing or Completed state to that the executable is able to exit
            this.ProgressChannelStore.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Failed) || allUpdates[fakeRun.Id].Contains(JobRunStates.Completed), 10000);
        }

        [TestMethod]
        public void PlannedJob_AfterStart_PidIsSet()
        {
            var config = GivenAMinimalConfiguration();
            config.JobRunnerExecutable = "Jobbr.Server.ForkedExecution.TestRunner.exe";

            this.GivenAStartedBackChannelHost(config);
            var executor = this.GivenAStartedExecutor(config);

            var fakeRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);

            // Act
            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            var hasConnected = this.ProgressChannelStore.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Connected), 3000);

            Assert.IsTrue(hasConnected, "The runner executable should connect within 1s");

            var hasPid = this.ProgressChannelStore.AllPids.ContainsKey(fakeRun.Id);

            Assert.IsTrue(hasPid, "There should be a Pid stored for this jobRun");
            Assert.IsTrue(this.ProgressChannelStore.AllPids[fakeRun.Id].First().Item2 > 0);
            Assert.IsFalse(string.IsNullOrWhiteSpace(this.ProgressChannelStore.AllPids[fakeRun.Id].First().Item1));
        }

        [TestMethod]
        public void PlannedJob_AfterStarting_FoldersExist()
        {
            // Setup
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetTempFileName().Replace(".", ""));

            var config = GivenAMinimalConfiguration();
            config.JobRunnerExecutable = "Jobbr.Server.ForkedExecution.TestEcho.exe";
            config.JobRunDirectory = tempDir;

            var executor = this.GivenAStartedExecutor(config);

            // Act
            var fakeRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);

            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            // Wait
            var hasStarted = this.ProgressChannelStore.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Starting), 3000);

            Assert.IsTrue(hasStarted, "The runner executable should have started, but did not within 3s");

            Assert.IsTrue(Directory.Exists(tempDir), "The temp directory should haven been created");
            Assert.IsTrue(Directory.Exists(Path.Combine(tempDir, $"jobbr-{fakeRun.Id}")), "The run directory should exist");
            Assert.IsTrue(Directory.Exists(Path.Combine(tempDir, $"jobbr-{fakeRun.Id}", "work")), "The run\\work directory should exist");
        }

        [TestMethod]
        public void RunnerExecutable_WithInvalidJob_ReturnsFailed()
        {
            // Setup
            var config = GivenAMinimalConfiguration();
            config.JobRunnerExecutable = "Jobbr.Server.ForkedExecution.TestRunner.exe";
            
            this.GivenAStartedBackChannelHost(config);
            var executor = this.GivenAStartedExecutor(config);

            var fakeRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            fakeRun.JobRunInfo.Type = "NotExistentClrType";

            // Act
            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            // Test
            var hasCompleted = this.ProgressChannelStore.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Failed), 3000);

            Assert.IsTrue(hasCompleted, "The runner executable should fail, but did not within 3s");
            Assert.IsTrue(this.ProgressChannelStore.AllStatusUpdates[fakeRun.Id].Contains(JobRunStates.Failed), "There should be a failed state for this jobRun");
        }

        [TestMethod]
        public void RunnerExecutable_GetsValidJob_ReturnsCompleted()
        {
            // Setup
            var config = GivenAMinimalConfiguration();
            config.JobRunnerExecutable = "Jobbr.Server.ForkedExecution.TestRunner.exe";

            this.GivenAStartedBackChannelHost(config);
            var executor = this.GivenAStartedExecutor(config);

            var fakeRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            fakeRun.JobRunInfo.Type = "SimpleJob";

            // Act
            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            // Test
            var hasCompleted = this.ProgressChannelStore.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Completed), 3000);

            Assert.IsTrue(hasCompleted, "The runner executable should completed, but did not within 3s");
            Assert.IsTrue(this.ProgressChannelStore.AllStatusUpdates[fakeRun.Id].Contains(JobRunStates.Completed), "There should be a completed state for this jobRun");
        }

        [TestMethod]
        public void RunnerExecutable_ParamsJobWithoutParams_Completes()
        {
            // Setup
            var config = GivenAMinimalConfiguration();
            config.JobRunnerExecutable = "Jobbr.Server.ForkedExecution.TestRunner.exe";

            this.GivenAStartedBackChannelHost(config);
            var executor = this.GivenAStartedExecutor(config);

            var fakeRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            fakeRun.JobRunInfo.Type = "JobWithParameters";
            
            // Act
            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            // Test
            var hasCompleted = this.ProgressChannelStore.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Completed), 3000);

            Assert.IsTrue(hasCompleted, "The runner executable should complete, but did not within 3s");
            Assert.IsTrue(this.ProgressChannelStore.AllStatusUpdates[fakeRun.Id].Contains(JobRunStates.Completed), "There should be a completed state for this jobRun");
        }

        [TestMethod]
        public void RunnerExecutable_JobWithArtefacts_UploadsFiles()
        {
            // Setup
            var config = GivenAMinimalConfiguration();
            config.JobRunnerExecutable = "Jobbr.Server.ForkedExecution.TestRunner.exe";

            this.GivenAStartedBackChannelHost(config);
            var executor = this.GivenAStartedExecutor(config);

            var fakeRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            fakeRun.JobRunInfo.Type = "JobWithArtefacts";

            // Act
            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            // Test
            var hasCompleted = this.ProgressChannelStore.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Completed), 3000);

            Assert.IsTrue(hasCompleted, "The runner executable should completed, but did not within 3s");
            Assert.IsTrue(this.ProgressChannelStore.AllUploadedArtefacts[fakeRun.Id].Any(), "It should contain at least one file!");
        }

        [TestMethod]
        public void RunnerExecutable_JobWithProgress_SendsMultipleStates()
        {
            // Setup
            var config = GivenAMinimalConfiguration();
            config.JobRunnerExecutable = "Jobbr.Server.ForkedExecution.TestRunner.exe";

            this.GivenAStartedBackChannelHost(config);
            var executor = this.GivenAStartedExecutor(config);

            var fakeRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            fakeRun.JobRunInfo.Type = "SimpleJob";

            // Act
            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            this.ProgressChannelStore.WaitForStatusUpdate(updatesFromAllJobs => updatesFromAllJobs[fakeRun.Id].Contains(JobRunStates.Completed), 3000);

            Assert.IsTrue(this.ProgressChannelStore.AllStatusUpdates[fakeRun.Id].Contains(JobRunStates.Connected), "It should contain the Connected state");
            Assert.IsTrue(this.ProgressChannelStore.AllStatusUpdates[fakeRun.Id].Contains(JobRunStates.Initializing), "It should contain the Initializing state");
            Assert.IsTrue(this.ProgressChannelStore.AllStatusUpdates[fakeRun.Id].Contains(JobRunStates.Processing), "It should contain the Processing state");
            Assert.IsTrue(this.ProgressChannelStore.AllStatusUpdates[fakeRun.Id].Contains(JobRunStates.Finishing), "It should contain the Finishing state");
            Assert.IsTrue(this.ProgressChannelStore.AllStatusUpdates[fakeRun.Id].Contains(JobRunStates.Collecting), "It should contain the Collecting state");
            Assert.IsTrue(this.ProgressChannelStore.AllStatusUpdates[fakeRun.Id].Contains(JobRunStates.Completed), "It should contain the Completed state");
        }

        [TestMethod]
        public void RunnerExecutable_JobWithProgress_MultipleStatus()
        {
            // Setup
            var config = GivenAMinimalConfiguration();
            config.JobRunnerExecutable = "Jobbr.Server.ForkedExecution.TestRunner.exe";

            this.GivenAStartedBackChannelHost(config);
            var executor = this.GivenAStartedExecutor(config);

            var fakeRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            fakeRun.JobRunInfo.Type = "JobWithOneProgress";

            // Act
            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            this.ProgressChannelStore.WaitForProgressUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobWithOneProgress.DefinedProgressValue), 3000);

            Assert.IsTrue(this.ProgressChannelStore.AllProgressUpdates[fakeRun.Id].Contains(JobWithOneProgress.DefinedProgressValue), "The runner executable should have send one progress update");
        }

        [TestMethod]
        public void RunnerExecutable_SuccessfulJobWithProgress_SetTo100Percent()
        {
            // Setup
            var config = GivenAMinimalConfiguration();
            config.JobRunnerExecutable = "Jobbr.Server.ForkedExecution.TestRunner.exe";

            this.GivenAStartedBackChannelHost(config);
            var executor = this.GivenAStartedExecutor(config);

            var fakeRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            fakeRun.JobRunInfo.Type = "JobWithOneProgress";

            // Act
            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            // Test
            var hasCompleted = this.ProgressChannelStore.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Completed), 3000);

            Assert.IsTrue(hasCompleted, "The runner executable should completed, but did not within 3s");
            this.ProgressChannelStore.WaitForProgressUpdate(allUpdates => allUpdates[fakeRun.Id].Count == 2, 3000);

            Assert.AreEqual(2, this.ProgressChannelStore.AllProgressUpdates[fakeRun.Id].Count, "Got: [" + string.Join(", ", this.ProgressChannelStore.AllProgressUpdates[fakeRun.Id]) + "]");
            Assert.AreEqual(JobRunStates.Completed, this.ProgressChannelStore.AllStatusUpdates[fakeRun.Id].Last(), "The job did not complete");
            Assert.AreEqual(100, this.ProgressChannelStore.AllProgressUpdates[fakeRun.Id][1], "It should have announced 100% completion after successful exit");
        }

        [TestMethod]
        public void RunnerExecutable_FailingJobWithProgress_DontTouchProgress()
        {
            // Setup
            var config = GivenAMinimalConfiguration();
            config.JobRunnerExecutable = "Jobbr.Server.ForkedExecution.TestRunner.exe";

            this.GivenAStartedBackChannelHost(config);
            var executor = this.GivenAStartedExecutor(config);

            var fakeRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            fakeRun.JobRunInfo.Type = "JobWithOneProgress";
            fakeRun.JobRunInfo.InstanceParameters = JsonConvert.SerializeObject(new { ShouldFail = true });

            // Act
            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            // Test
            var hasCompleted = this.ProgressChannelStore.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Failed), 3000);

            Assert.IsTrue(hasCompleted, "The runner executable should have failed in the meantime, but did not within 3s");

            this.ProgressChannelStore.WaitForProgressUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobWithOneProgress.DefinedProgressValue), 3000);

            Assert.IsTrue(this.ProgressChannelStore.AllProgressUpdates.ContainsKey(fakeRun.Id), "There should be any progress update");
            Assert.AreEqual(1, this.ProgressChannelStore.AllProgressUpdates[fakeRun.Id].Count, "Got: [" + string.Join(", ", this.ProgressChannelStore.AllProgressUpdates[fakeRun.Id]) + "]");
            Assert.AreEqual(JobRunStates.Failed, this.ProgressChannelStore.AllStatusUpdates[fakeRun.Id].Last(), "The job did not complete");
        }

        [TestMethod]
        public void RunnerExecutable_AdditionalParameters_PassedToExecutable()
        {
            // Setup
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetTempFileName().Replace(".", ""));

            var config = GivenAMinimalConfiguration();
            config.JobRunnerExecutable = "Jobbr.Server.ForkedExecution.TestEcho.exe";
            config.JobRunDirectory = tempDir;
            config.AddJobRunnerArguments = infos => new List<KeyValuePair<string, string>>(new []{ new KeyValuePair<string, string>("argument1", "value1"), });

            this.GivenAStartedBackChannelHost(config);
            var executor = this.GivenAStartedExecutor(config);

            var fakeRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            var expectedWorkdir = Path.Combine(tempDir, $"jobbr-{fakeRun.Id}", "work");

            // Act
            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            // Wait until process has exited
            var hasCompleted = this.ProgressChannelStore.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Completed), 3000);

            Assert.IsTrue(hasCompleted, "The Echo Executable did not complete withing the timeout");
            Assert.IsTrue(Directory.Exists(expectedWorkdir), "The run\\work directory should exist");
            Assert.IsTrue(Directory.EnumerateFiles(expectedWorkdir).Any(), "There should be one file");

            var fileContent = File.ReadAllText(Directory.EnumerateFiles(expectedWorkdir).First());
            Assert.IsNotNull(fileContent);
            Assert.IsTrue(fileContent.Contains("--argument1\nvalue1"), $"Expected to find arguments in content, but got '{fileContent}'");
        }

        [TestMethod]
        public void RunnerExecutable_WhiteSpaceParameters_PassedToExecutable()
        {
            // Setup
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetTempFileName().Replace(".", ""));

            var config = GivenAMinimalConfiguration();
            config.JobRunnerExecutable = "Jobbr.Server.ForkedExecution.TestEcho.exe";
            config.JobRunDirectory = tempDir;
            config.AddJobRunnerArguments = infos => new List<KeyValuePair<string, string>>(new[] { new KeyValuePair<string, string>("arg", "v is with wthitespaced"), });

            this.GivenAStartedBackChannelHost(config);
            var executor = this.GivenAStartedExecutor(config);

            var fakeRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            var expectedWorkdir = Path.Combine(tempDir, $"jobbr-{fakeRun.Id}", "work");

            // Act
            this.manualTimeProvider.AddSecond();
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            // Wait until process has exited
            var hasCompleted = this.ProgressChannelStore.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Completed), 3000);

            Assert.IsTrue(hasCompleted, "The Echo Executable did not complete withing the timeout");
            Assert.IsTrue(Directory.Exists(expectedWorkdir), "The run\\work directory should exist");
            Assert.IsTrue(Directory.EnumerateFiles(expectedWorkdir).Any(), "There should be one file");

            var fileContent = File.ReadAllText(Directory.EnumerateFiles(expectedWorkdir).First());
            Assert.IsNotNull(fileContent);
            Assert.IsTrue(fileContent.Contains("--arg\nv is with wthitespaced"), $"Expected to find arguments in content, but got '{fileContent}'");
        }
    }
}
