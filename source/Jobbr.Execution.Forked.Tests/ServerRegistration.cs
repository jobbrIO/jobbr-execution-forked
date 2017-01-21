using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Jobbr.Server.Builder;
using Jobbr.Server.ForkedExecution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Execution.Forked.Tests
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
        public void WhenComponentIsRegistered_ServerStarts_EndpointIsAvailable()
        {
            var backendAddress = "http://localhost:" + NextFreeTcpPort();

            var builder = new JobbrBuilder();
            builder.AddForkedExecution(config =>
            {
                config.BackendAddress = backendAddress;
            });

            var server = builder.Create();

            server.Start();

            var statusResponse = new HttpClient().GetAsync(backendAddress + "/fex/status").Result;

            Assert.AreEqual(HttpStatusCode.OK, statusResponse.StatusCode);
        }
    }
}
