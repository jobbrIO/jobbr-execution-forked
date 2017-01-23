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

        public void PlannedJob_AfterStart_WillCallback()
        {
        }
    }
}
