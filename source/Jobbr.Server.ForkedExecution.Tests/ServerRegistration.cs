using System.IO;
using System.Net;
using System.Net.Http;
using Jobbr.Server.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class ServerRegistration
    {
        [TestMethod]
        public void WithInMemoryServer_ServerHasStarted_StatusEndpointIsAvailable()
        {
            var backendAddress = "http://localhost:" + TcpPortHelper.NextFreeTcpPort();

            var builder = new JobbrBuilder();
            builder.AddForkedExecution(config =>
            {
                config.BackendAddress = backendAddress;
                config.JobRunDirectory = Path.GetTempPath();
                config.JobRunnerExecutable = @"c:\windows\System32\cmd.exe";
            });

            var server = builder.Create();

            server.Start();

            var statusResponse = new HttpClient().GetAsync(backendAddress + "/fex/status").Result;

            Assert.AreEqual(HttpStatusCode.OK, statusResponse.StatusCode);
        }
    }
}
