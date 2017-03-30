using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Jobbr.Server.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class ServerRegistration
    {
        private static int NextFreeTcpPort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        [TestMethod]
        public void WithInMemoryServer_ServerHasStarted_StatusEndpointIsAvailable()
        {
            var backendAddress = "http://localhost:" + NextFreeTcpPort();

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

        [TestMethod]
        public void WithInMemoryServer_ServerHasStarted_JobRunInfoEndpointIsAvailable()
        {
            var backendAddress = "http://localhost:" + NextFreeTcpPort();

            var builder = new JobbrBuilder();
            builder.AddForkedExecution(config =>
            {
                config.BackendAddress = backendAddress;
                config.JobRunDirectory = Path.GetTempPath();
                config.JobRunnerExecutable = @"c:\windows\System32\cmd.exe";
            });

            var server = builder.Create();

            server.Start();

            var statusResponse = new HttpClient().GetAsync(backendAddress + "/fex/jobrun/524868244").Result;

            Assert.AreEqual(HttpStatusCode.NotFound, statusResponse.StatusCode);
        }
    }
}
