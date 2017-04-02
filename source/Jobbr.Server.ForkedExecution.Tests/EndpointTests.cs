using System.IO;
using System.Net;
using System.Net.Http;
using Jobbr.Server.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class EndpointTests
    {
        private readonly string configBackendAddress;

        public EndpointTests()
        {
            this.configBackendAddress = "http://localhost:" + TcpPortHelper.NextFreeTcpPort();
        }

        [TestMethod]
        public void WithInMemoryServer_ServerHasStarted_JobRunInfoEndpointIsAvailable()
        {
            this.GivenARunningServer();

            var statusResponse = this.CallUrl("/fex/jobrun/524868244");

            Assert.AreEqual(HttpStatusCode.NotFound, statusResponse.StatusCode);
        }

        private HttpResponseMessage CallUrl(string url)
        {
            return new HttpClient().GetAsync(this.configBackendAddress + url).Result;
        }

        private void GivenARunningServer()
        {
            var builder = new JobbrBuilder();
            builder.AddForkedExecution(config =>
            {
                config.BackendAddress = this.configBackendAddress;
                config.JobRunDirectory = Path.GetTempPath();
                config.JobRunnerExecutable = @"c:\windows\System32\cmd.exe";
            });

            var server = builder.Create();

            server.Start();
        }
    }
}
