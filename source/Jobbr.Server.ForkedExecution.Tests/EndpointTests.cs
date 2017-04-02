using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Jobbr.Server.Builder;
using Jobbr.Server.ForkedExecution.BackChannel;
using Jobbr.Server.ForkedExecution.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class EndpointTests
    {
        private readonly string configBackendAddress;
        private FakeGeneratedJobRunsStore fakeStore = new FakeGeneratedJobRunsStore();
        private JobRunProgressUpdateStore updateFakeStore = new JobRunProgressUpdateStore();

        public EndpointTests()
        {
            this.configBackendAddress = "http://localhost:" + TcpPortHelper.NextFreeTcpPort();
        }

        [TestMethod]
        public void JobInfoEndpoint_GetNonExistingById_ReturnsNotFound()
        {
            this.GivenARunningServer();

            var statusResponse = this.Get("/fex/jobrun/524868244");

            Assert.AreEqual(HttpStatusCode.NotFound, statusResponse.StatusCode);
        }

        [TestMethod]
        public void JobInfoEndpoint_GetExistingById_ReturnsOk()
        {
            this.GivenARunningServer();
            var createdJobRun = this.fakeStore.CreateFakeJobRun(DateTime.UtcNow);

            var statusResponse = this.Get("/fex/jobrun/" + createdJobRun.Id);

            Assert.AreEqual(HttpStatusCode.OK, statusResponse.StatusCode);
        }

        [TestMethod]
        public void JobRunEndpoint_UpdateExisting_ReturnsAccepted()
        {
            this.GivenARunningServer();
            var createdJobRun = this.fakeStore.CreateFakeJobRun(DateTime.UtcNow);

            var statusResponse = this.Put("/fex/jobrun/" + createdJobRun.Id, new { State = "Started" });

            Assert.AreEqual(HttpStatusCode.Accepted, statusResponse.StatusCode);
            Assert.AreEqual(1, this.updateFakeStore.AllStatusUpdates.SelectMany(u => u.Value).Count(), "There should be one single update");
        }

        [TestMethod]
        public void JobRunEndpoint_UpdateNotExisting_ReturnsNotFound()
        {
            this.GivenARunningServer();

            var statusResponse = this.Put("/fex/jobrun/524868244", new { });

            Assert.AreEqual(HttpStatusCode.NotFound, statusResponse.StatusCode);
        }

        [TestMethod]
        public void JobRunEndpoint_SendInvalidState_ReturnsNotFound()
        {
            this.GivenARunningServer();
            var createdJobRun = this.fakeStore.CreateFakeJobRun(DateTime.UtcNow);
            var statusResponse = this.Put("/fex/jobrun/" + createdJobRun.Id, new { State = "MyCustomState" });

            Assert.AreEqual(HttpStatusCode.BadRequest, statusResponse.StatusCode);
        }

        [TestMethod]
        public void ArtefactEndpoint_SendForKnownJobRun_ReturnsAccepted()
        {
            this.GivenARunningServer();
            var createdJobRun = this.fakeStore.CreateFakeJobRun(DateTime.UtcNow);

            var statusResponse = this.SendImage("/fex/jobrun/" + createdJobRun.Id + "/artefacts", new MemoryStream());

            Assert.AreEqual(HttpStatusCode.Accepted, statusResponse.StatusCode);
            Assert.AreEqual(1, this.updateFakeStore.AllUploadedArtefacts.SelectMany(a => a.Value).Count());
        }

        [TestMethod]
        public void ArtefactEndpoint_SendForUnknownJobRun_ReturnsNotFound()
        {
            this.GivenARunningServer();

            var statusResponse = this.SendImage("/fex/jobrun/524868244/artefacts", new MemoryStream());

            Assert.AreEqual(HttpStatusCode.NotFound, statusResponse.StatusCode);
        }

        private HttpResponseMessage Get(string url)
        {
            return new HttpClient().GetAsync(this.configBackendAddress + url).Result;
        }

        private HttpResponseMessage Put(string url, object json)
        {
            return new HttpClient().PutAsJsonAsync(this.configBackendAddress + url, json).Result;
        }

        private HttpResponseMessage SendImage(string url, MemoryStream fileStream)
        {
            var requestContent = new MultipartFormDataContent();
            //    here you can specify boundary if you need---^
            var imageContent = new ByteArrayContent(fileStream.ToArray());
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");

            requestContent.Add(imageContent, "image", "image.jpg");

            return new HttpClient().PostAsync(this.configBackendAddress + url, requestContent).Result;
        }

        private void GivenARunningServer()
        {
            var config = new ForkedExecutionConfiguration()
            {
                BackendAddress = this.configBackendAddress
            };

            var webHost = new BackChannelWebHost(new JobbrServiceProviderMock(new JobRunInfoServiceMock(this.fakeStore), this.updateFakeStore), config);

            webHost.Start();
        }
    }
}
