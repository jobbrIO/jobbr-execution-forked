using System;
using Jobbr.Server.Builder;
using Jobbr.Server.ForkedExecution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Execution.Forked.Tests
{
    [TestClass]
    public class ServerRegistration
    {
        [TestMethod]
        public void WhenComponentIsRegistered_ServerStarts_EndpointIsAvailable()
        {
            var builder = new JobbrBuilder();

            builder.AddForkedExecution();

            var server = builder.Create();

            server.Start();
        }
    }
}
