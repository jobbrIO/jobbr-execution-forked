using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Jobbr.Server.ForkedExecution.BackChannel;
using Jobbr.Server.ForkedExecution.Tests.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class BackChannelWebHostTests
    {
        private LoggerFactory _loggerFactory;

        [TestInitialize()]
        public void Startup()
        {
            _loggerFactory = new LoggerFactory();
        }

        [TestMethod]
        public void BackChannel_StartWebHost_StatusUrlIsAvailable()
        {
            var forkedExecutionConfiguration = new ForkedExecutionConfiguration
            {
                BackendAddress = "http://localhost:" + TcpPortHelper.NextFreeTcpPort()
            };

            var host = new BackChannelWebHost(_loggerFactory, new JobbrServiceProviderMock(_loggerFactory, null, null), forkedExecutionConfiguration);

            host.Start();

            var response = new HttpClient().GetAsync(forkedExecutionConfiguration.BackendAddress + "/fex/status").Result;

            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        [TestMethod]
        public async Task BackChannel_AfterStop_IsNotAvailable()
        {
            var forkedExecutionConfiguration = new ForkedExecutionConfiguration
            {
                BackendAddress = "http://localhost:" + TcpPortHelper.NextFreeTcpPort()
            };

            var host = new BackChannelWebHost(_loggerFactory, new JobbrServiceProviderMock(_loggerFactory, null, null), forkedExecutionConfiguration);

            host.Start();
            host.Stop();

            try
            {
                await new HttpClient().GetAsync(forkedExecutionConfiguration.BackendAddress + "/fex/status");
            }
            catch (Exception ex)
            {
                if (ex.InnerException is WebException == false)
                {
                    Assert.Fail("Exception thrown was " + ex.InnerException + ", which is not the expected exception");
                }
            }
        }

        [TestMethod]
        public async Task BackChannel_AfterDisposal_IsNotAvailable()
        {
            var forkedExecutionConfiguration = new ForkedExecutionConfiguration
            {
                BackendAddress = "http://localhost:" + TcpPortHelper.NextFreeTcpPort()
            };

            var host = new BackChannelWebHost(_loggerFactory, new JobbrServiceProviderMock(_loggerFactory, null, null), forkedExecutionConfiguration);
            
            host.Start();
            host.Dispose();
            try
            {
                await new HttpClient().GetAsync(forkedExecutionConfiguration.BackendAddress + "/fex/status");
            }
            catch (Exception ex)
            {
                if (ex.InnerException is WebException == false)
                {
                    Assert.Fail("Exception thrown was " + ex.InnerException + ", which is not the expected exception");
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void BackChannel_StartedTwice_RaisesException()
        {
            var forkedExecutionConfiguration = new ForkedExecutionConfiguration
            {
                BackendAddress = "http://localhost:" + TcpPortHelper.NextFreeTcpPort()
            };

            var host = new BackChannelWebHost(_loggerFactory, new JobbrServiceProviderMock(_loggerFactory, null, null), forkedExecutionConfiguration);

            host.Start();
            host.Start();
        }

        [TestMethod]
        [ExpectedException(typeof(HttpListenerException))]
        public void BackChannel_PortInUse_RaisesException()
        {
            var nextFreeTcpPort = TcpPortHelper.NextFreeTcpPort();

            // intentionally block port
            new TcpListener(IPAddress.Any, nextFreeTcpPort).Start();

            var forkedExecutionConfiguration = new ForkedExecutionConfiguration
            {
                BackendAddress = "http://localhost:" + nextFreeTcpPort
            };

            var host = new BackChannelWebHost(_loggerFactory, new JobbrServiceProviderMock(_loggerFactory, null, null), forkedExecutionConfiguration);

            host.Start();
        }
    }
}
