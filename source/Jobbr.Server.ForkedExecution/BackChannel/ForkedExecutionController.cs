using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Execution.Model;
using Jobbr.Server.ForkedExecution.BackChannel.Model;
using Jobbr.Server.ForkedExecution.Logging;
using Newtonsoft.Json;

namespace Jobbr.Server.ForkedExecution.BackChannel
{
    [RoutePrefix("fex")]
    public class ForkedExecutionController : ApiController
    {
        private static readonly ILog Logger = LogProvider.For<ForkedExecutionController>();

        private readonly IJobRunInformationService jobRunInformationService;
        private readonly IJobRunProgressChannel progressChannel;

        public ForkedExecutionController(IJobRunInformationService jobRunInformationService, IJobRunProgressChannel progressChannel)
        {
            this.jobRunInformationService = jobRunInformationService;
            this.progressChannel = progressChannel;
        }

        [HttpGet]
        [Route("jobrun/{jobRunId}")]
        public IHttpActionResult GetJonbRunInfos(long jobRunId)
        {
            Logger.Debug($"ConsoleExecutor is requesting information about JobRun with id '{jobRunId}'");

            var jobRun = this.jobRunInformationService.GetByJobRunId(jobRunId);

            if (jobRun == null)
            {
                Logger.Warn($"JobRun Information for JobRun with '{jobRunId}' cannot be found.");
                return this.NotFound();
            }

            var infoDto = new JobRunInfoDto
            {
                JobId = jobRun.JobId,
                TriggerId = jobRun.TriggerId,
                UserId = jobRun.UserId,
                Username = jobRun.Username,
                JobRunId = jobRunId,
                JobName = jobRun.UniqueName,
                JobType = jobRun.Type,
                TempDir = jobRun.TempDir,
                WorkingDir = jobRun.WorkingDir,
                UniqueId = jobRun.UniqueId,
                JobParameter = jobRun.JobParameters != null ? JsonConvert.DeserializeObject(jobRun.JobParameters) : null,
                InstanceParameter = jobRun.InstanceParameters != null ? JsonConvert.DeserializeObject(jobRun.InstanceParameters) : null,
            };

            Logger.Debug($"Returning JobRun information for JobRun '{jobRun.UniqueId}' with '{jobRunId}'");

            return this.Ok(infoDto);
        }

        [HttpPut]
        [Route("jobrun/{jobRunId}")]
        public IHttpActionResult PutJobRunUpdate(long jobRunId, [FromBody] JobRunUpdateDto dto)
        {
            Logger.Debug($"ConsoleExecutor is trying to update job with id '{jobRunId}'");
            var jobRun = this.jobRunInformationService.GetByJobRunId(jobRunId);

            if (jobRun == null)
            {
                Logger.Warn($"JobRun with id '{jobRunId}' cannot be found.");
                return this.NotFound();
            }

            if (dto.State == JobRunStates.Null)
            {
                Logger.Warn($"JobRun with id '{jobRunId}' sent an invalid state which defaulted to 'None' but still cannot be accepted.");
                return this.BadRequest("Invalid state");
            }

            Logger.Info($"Publishing state update '{dto.State}' for JobRun '{jobRun.UniqueId}' with '{jobRunId}'");
            this.progressChannel.PublishStatusUpdate(jobRun, dto.State);

            return this.StatusCode(HttpStatusCode.Accepted);
        }

        [HttpPost]
        [Route("jobrun/{jobRunId}/artefacts")]
        public IHttpActionResult AddArtefacts(long jobRunId)
        {
            Logger.Debug($"ConsoleExecutor is upload jobartefacts for job with id '{jobRunId}'");
            var jobRun = this.jobRunInformationService.GetByJobRunId(jobRunId);

            if (jobRun == null)
            {
                Logger.Warn($"JobRun with id '{jobRunId}' cannot be found.");
                return this.NotFound();
            }

            IEnumerable<HttpContent> parts = this.Request.Content.ReadAsMultipartAsync().Result.Contents;

            foreach (var part in parts)
            {
                var contentDisposition = part.Headers.ContentDisposition;

                var result = part.ReadAsStreamAsync().Result;

                Logger.Info($"Publishing jobrun artefact '{contentDisposition.FileName}' for JobRun '{jobRun.UniqueId}' with '{result.Length}' bytes");
                this.progressChannel.PublicArtefact(jobRun.UniqueId, contentDisposition.FileName, result);
            }

            return this.StatusCode(HttpStatusCode.Accepted);
        }
    }
}
