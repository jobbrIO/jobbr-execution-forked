using System.Net;
using System.Net.Http;
using Jobbr.Server.ForkedExecution.BackChannel;
using Jobbr.Server.ForkedExecution.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class ExecutionTests : TestBase
    {
        private void GivenAStartedBackChannelHost(ForkedExecutionConfiguration config)
        {
            config.BackendAddress = string.Empty;

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
            config.JobRunnerExeResolver = () => "Jobbr.Server.ForkedExecution.TestRunner.exe";

            this.GivenAStartedBackChannelHost(config);
            var executor = this.GivenAStartedExecutor(config);

            var fakeRun = this.jobRunFakeTuples.CreateFakeJobRun();

            // Act
            executor.OnPlanChanged(new List<PlannedJobRun>(new [] { fakeRun.PlannedJobRun }));

            // Test
            var hasConnected = this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.UniqueId].Contains(JobRunStates.Connected), 1000);

            Assert.IsTrue(hasConnected, "The runner executable should connect within 3s");

            // Tearddown: Wait for Failing or Completed state to that the executable is able to exit
            this.storedProgressUpdates.WaitForStatusUpdate(allUpdates => allUpdates[fakeRun.UniqueId].Contains(JobRunStates.Failed) || allUpdates[fakeRun.UniqueId].Contains(JobRunStates.Completed), 10000);
        }

        }
    }
}
