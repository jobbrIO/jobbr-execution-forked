using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Jobbr.Server.ForkedExecution.BackChannel;
using Jobbr.Server.ForkedExecution.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class EndpointTests
    {
        private readonly string _configBackendAddress;
        private readonly FakeGeneratedJobRunsStore _fakeStore = new();
        private readonly ProgressChannelStore _channelFakeStore = new();

        public EndpointTests()
        {
            _configBackendAddress = $"http://localhost:{TcpPortHelper.NextFreeTcpPort()}";
        }

        [TestMethod]
        public void JobInfoEndpoint_GetNonExistingById_ReturnsNotFound()
        {
            GivenARunningServer();

            var statusResponse = Get("/fex/jobrun/524868244");

            Assert.AreEqual(HttpStatusCode.NotFound, statusResponse.StatusCode);
        }

        [TestMethod]
        public void JobInfoEndpoint_GetExistingById_ReturnsOk()
        {
            GivenARunningServer();
            var createdJobRun = _fakeStore.CreateFakeJobRun(DateTime.UtcNow);

            var statusResponse = Get("/fex/jobrun/" + createdJobRun.Id);

            Assert.AreEqual(HttpStatusCode.OK, statusResponse.StatusCode);
        }

        [TestMethod]
        public void JobRunEndpoint_UpdateExisting_ReturnsAccepted()
        {
            GivenARunningServer();
            var createdJobRun = _fakeStore.CreateFakeJobRun(DateTime.UtcNow);

            var statusResponse = Put("/fex/jobrun/" + createdJobRun.Id, new { State = "Started" });

            Assert.AreEqual(HttpStatusCode.Accepted, statusResponse.StatusCode);
            Assert.AreEqual(1, _channelFakeStore.AllStatusUpdates.SelectMany(u => u.Value).Count(), "There should be one single update");
        }

        [TestMethod]
        public void JobRunEndpoint_UpdateNotExisting_ReturnsNotFound()
        {
            GivenARunningServer();

            var statusResponse = Put("/fex/jobrun/524868244", new { });

            Assert.AreEqual(HttpStatusCode.NotFound, statusResponse.StatusCode);
        }

        [TestMethod]
        public void JobRunEndpoint_SendInvalidState_ReturnsNotFound()
        {
            GivenARunningServer();
            var createdJobRun = _fakeStore.CreateFakeJobRun(DateTime.UtcNow);
            var statusResponse = Put("/fex/jobrun/" + createdJobRun.Id, new { State = "MyCustomState" });

            Assert.AreEqual(HttpStatusCode.BadRequest, statusResponse.StatusCode);
        }

        [TestMethod]
        public void ArtefactEndpoint_SendForKnownJobRun_ReturnsAccepted()
        {
            GivenARunningServer();
            var createdJobRun = _fakeStore.CreateFakeJobRun(DateTime.UtcNow);

            var statusResponse = SendImage("/fex/jobrun/" + createdJobRun.Id + "/artefacts", new MemoryStream());

            Assert.AreEqual(HttpStatusCode.Accepted, statusResponse.StatusCode);
            Assert.AreEqual(1, _channelFakeStore.AllUploadedArtefacts.SelectMany(a => a.Value).Count());
        }

        [TestMethod]
        public void ArtefactEndpoint_SendForUnknownJobRun_ReturnsNotFound()
        {
            GivenARunningServer();

            var statusResponse = SendImage("/fex/jobrun/524868244/artefacts", new MemoryStream());

            Assert.AreEqual(HttpStatusCode.NotFound, statusResponse.StatusCode);
        }

        private HttpResponseMessage Get(string url)
        {
            return new HttpClient().GetAsync(_configBackendAddress + url).Result;
        }

        private HttpResponseMessage Put(string url, object json)
        {
            return new HttpClient().PutAsJsonAsync(_configBackendAddress + url, json).Result;
        }

        private HttpResponseMessage SendImage(string url, MemoryStream fileStream)
        {
            var requestContent = new MultipartFormDataContent();
            //    here you can specify boundary if you need---^
            var imageContent = new ByteArrayContent(fileStream.ToArray());
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");

            requestContent.Add(imageContent, "image", "image.jpg");

            return new HttpClient().PostAsync(_configBackendAddress + url, requestContent).Result;
        }

        private void GivenARunningServer()
        {
            var config = new ForkedExecutionConfiguration()
            {
                BackendAddress = _configBackendAddress
            };

            var webHost = new BackChannelWebHost(new NullLoggerFactory(), new JobbrServiceProviderMock(new NullLoggerFactory(), new JobRunInfoServiceMock(_fakeStore), _channelFakeStore), config);

            webHost.Start();
        }
    }
}
