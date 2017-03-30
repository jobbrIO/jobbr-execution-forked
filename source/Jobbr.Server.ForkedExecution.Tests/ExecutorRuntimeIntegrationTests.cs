using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Jobbr.ComponentModel.Execution.Model;
using Jobbr.Server.ForkedExecution.BackChannel;
using Jobbr.Server.ForkedExecution.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class ExecutorRuntimeIntegrationTests : TestBase
    {
        private void GivenAStartedBackChannelHost(ForkedExecutionConfiguration config)
        {
            config.BackendAddress = string.Empty;

            new ConfigurationValidator().Validate(config);

            var backChannelHost = new BackChannelWebHost(new JobbrServiceProviderMock(this.jobRunInformationService, this.storedProgressUpdates), config);
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
            executor.OnPlanChanged(new List<PlannedJobRun>(new [] { fakeRun.PlannedJobRun }));

            // Test
            var hasConnected = this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Connected), 1000);

            Assert.IsTrue(hasConnected, "The runner executable should connect within 1s");

            // Tearddown: Wait for Failing or Completed state to that the executable is able to exit
            this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Failed) || allUpdates[fakeRun.Id].Contains(JobRunStates.Completed), 10000);
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
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            // Wait
            var hasStarted = this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Starting), 3000);

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
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            // Test
            var hasCompleted = this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Failed), 3000);

            Assert.IsTrue(hasCompleted, "The runner executable should fail, but did not within 3s");
            Assert.IsTrue(this.storedProgressUpdates.AllStatusUpdates[fakeRun.Id].Contains(JobRunStates.Failed), "There should be a failed state for this jobRun");
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
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            // Test
            var hasCompleted = this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Completed), 3000);

            Assert.IsTrue(hasCompleted, "The runner executable should completed, but did not within 3s");
            Assert.IsTrue(this.storedProgressUpdates.AllStatusUpdates[fakeRun.Id].Contains(JobRunStates.Completed), "There should be a completed state for this jobRun");
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
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            // Test
            var hasCompleted = this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Completed), 3000);

            Assert.IsTrue(hasCompleted, "The runner executable should completed, but did not within 3s");
            Assert.IsTrue(this.storedProgressUpdates.AllUploadedArtefacts[fakeRun.Id].Any(), "It should contain at least one file!");
        }

        [TestMethod]
        public void RunnerExecutable_JobWithProgress_SendsPogress()
        {
            // Setup
            var config = GivenAMinimalConfiguration();
            config.JobRunnerExecutable = "Jobbr.Server.ForkedExecution.TestRunner.exe";

            this.GivenAStartedBackChannelHost(config);
            var executor = this.GivenAStartedExecutor(config);

            var fakeRun = this.jobRunFakeTuples.CreateFakeJobRun(DateTime.UtcNow);
            fakeRun.JobRunInfo.Type = "JobWithOneProgress";

            // Act
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            // Test
            var hasCompleted = this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Completed), 3000);
            this.storedProgressUpdates.WaitForProgressUpdate(allUpdates => allUpdates[fakeRun.Id].Any(), 3000);

            Assert.IsTrue(hasCompleted, "The runner executable should completed, but did not within 3s");

            Assert.IsTrue(this.storedProgressUpdates.AllProgressUpdates.ContainsKey(fakeRun.Id), "There should be progress updates from the run");
            Assert.AreEqual(1, this.storedProgressUpdates.AllProgressUpdates[fakeRun.Id].Count, "There should be exact one progress update!");
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
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            // Wait until process has exited
            var hasCompleted = this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Completed), 3000);

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
            executor.OnPlanChanged(new List<PlannedJobRun>(new[] { fakeRun.PlannedJobRun }));

            // Wait until process has exited
            var hasCompleted = this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.Id].Contains(JobRunStates.Completed), 3000);

            Assert.IsTrue(hasCompleted, "The Echo Executable did not complete withing the timeout");
            Assert.IsTrue(Directory.Exists(expectedWorkdir), "The run\\work directory should exist");
            Assert.IsTrue(Directory.EnumerateFiles(expectedWorkdir).Any(), "There should be one file");

            var fileContent = File.ReadAllText(Directory.EnumerateFiles(expectedWorkdir).First());
            Assert.IsNotNull(fileContent);
            Assert.IsTrue(fileContent.Contains("--arg\nv is with wthitespaced"), $"Expected to find arguments in content, but got '{fileContent}'");
        }
    }
}
