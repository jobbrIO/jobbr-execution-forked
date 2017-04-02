using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Jobbr.Server.ForkedExecution.BackChannel;
using Jobbr.Server.ForkedExecution.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class BackChannelWebHostTests
    {
        [TestMethod]
        public void BackChannel_StartWebHost_StatusUrlIsAvailable()
        {
            var forkedExecutionConfiguration = new ForkedExecutionConfiguration
            {
                BackendAddress = "http://localhost:" + TcpPortHelper.NextFreeTcpPort()
            };

            var host = new BackChannelWebHost(new JobbrServiceProviderMock(null, null), forkedExecutionConfiguration);

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

            var host = new BackChannelWebHost(new JobbrServiceProviderMock(null, null), forkedExecutionConfiguration);

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

            var host = new BackChannelWebHost(new JobbrServiceProviderMock(null, null), forkedExecutionConfiguration);

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

            var host = new BackChannelWebHost(new JobbrServiceProviderMock(null, null), forkedExecutionConfiguration);

            host.Start();
            host.Start();
        }
    }
}
