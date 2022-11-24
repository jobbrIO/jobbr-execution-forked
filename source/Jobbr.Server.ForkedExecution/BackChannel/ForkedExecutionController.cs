using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Execution.Model;
using Jobbr.Server.ForkedExecution.BackChannel.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Jobbr.Server.ForkedExecution.BackChannel
{
    /// <summary>
    /// Controller for controlling the execution of jobs.
    /// </summary>
    [ApiController]
    [Route("fex")]
    public class ForkedExecutionController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IJobRunInformationService _jobRunInformationService;
        private readonly IJobRunProgressChannel _progressChannel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForkedExecutionController"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="jobRunInformationService">Service for getting information about the job runs.</param>
        /// <param name="progressChannel">Channel for job run progress information.</param>
        public ForkedExecutionController(ILoggerFactory loggerFactory, IJobRunInformationService jobRunInformationService, IJobRunProgressChannel progressChannel)
        {
            _logger = loggerFactory.CreateLogger<ForkedExecutionController>();
            _jobRunInformationService = jobRunInformationService;
            _progressChannel = progressChannel;
        }

        /// <summary>
        /// Get information on job run based on ID.
        /// </summary>
        /// <param name="jobRunId">The target job run ID.</param>
        /// <returns>A result containing a <see cref="JobRunInfoDto"/> or 404 if not found.</returns>
        [HttpGet]
        [Route("jobrun/{jobRunId}")]
        public IActionResult GetJobRunInfos(long jobRunId)
        {
            _logger.LogDebug("ConsoleExecutor is requesting information about a job run with id '{jobRunId}'", jobRunId);

            var jobRun = _jobRunInformationService.GetByJobRunId(jobRunId);

            if (jobRun == null)
            {
                _logger.LogWarning("Job run information for job run with '{jobRunId}' cannot be found.", jobRunId);
                return NotFound();
            }

            var infoDto = new JobRunInfoDto
            {
                JobId = jobRun.JobId,
                TriggerId = jobRun.TriggerId,
                UserId = jobRun.UserId,
                UserDisplayName = jobRun.UserDisplayName,
                JobRunId = jobRunId,
                JobName = jobRun.UniqueName,
                JobType = jobRun.Type,
                JobParameter = jobRun.JobParameters != null ? JsonConvert.DeserializeObject(jobRun.JobParameters) : null,
                InstanceParameter = jobRun.InstanceParameters != null ? JsonConvert.DeserializeObject(jobRun.InstanceParameters) : null,
            };

            _logger.LogDebug("Returning job run information for job run '{jobRunId}'", jobRunId);

            return Ok(infoDto);
        }

        /// <summary>
        /// Updates job run state.
        /// </summary>
        /// <param name="jobRunId">The ID of the target job run.</param>
        /// <param name="dto">The payload for updating a job run with.</param>
        /// <returns>A result containg either 202(Accepted), 404(NotFound) or 400(BadRequest).</returns>
        [HttpPut]
        [Route("jobrun/{jobRunId}")]
        public IActionResult PutJobRunUpdate(long jobRunId, [FromBody] JobRunUpdateDto dto)
        {
            _logger.LogDebug("ConsoleExecutor is trying to update job with ID '{jobRunId}'", jobRunId);
            var jobRun = _jobRunInformationService.GetByJobRunId(jobRunId);

            if (jobRun == null)
            {
                _logger.LogWarning("Job run with ID '{jobRunId}' cannot be found.", jobRunId);
                return NotFound();
            }

            if (dto.State == JobRunStates.Null)
            {
                _logger.LogWarning("Job run with ID '{jobRunId}' sent an invalid state which defaulted to 'None' but still cannot be accepted.", jobRunId);
                return BadRequest("Invalid state");
            }

            _logger.LogInformation("Publishing state update '{state}' for job run ID '{jobRunId}'", dto.State, jobRunId);
            _progressChannel.PublishStatusUpdate(jobRun.Id, dto.State);

            return StatusCode((int)HttpStatusCode.Accepted);
        }

        /// <summary>
        /// Adds an artifact to the job run.
        /// </summary>
        /// <param name="jobRunId">The target job run ID.</param>
        /// <returns>A result that contains NotFound(404) or Accepted(202).</returns>
        [HttpPost]
        [Route("jobrun/{jobRunId}/artefacts")]
        public IActionResult AddArtefacts(long jobRunId)
        {
            _logger.LogDebug("ConsoleExecutor is upload job artifacts for job with ID '{jobRunId}'", jobRunId);
            var jobRun = _jobRunInformationService.GetByJobRunId(jobRunId);

            if (jobRun == null)
            {
                _logger.LogWarning("Job run with ID '{jobRunId}' cannot be found.", jobRunId);
                return NotFound();
            }

            // IEnumerable<HttpContent> parts = Request.Content.ReadAsMultipartAsync().Result.Contents; // TODO: find replacement
            IEnumerable<HttpContent> parts = new List<HttpContent>();

            foreach (var part in parts)
            {
                var contentDisposition = part.Headers.ContentDisposition;

                var result = part.ReadAsStreamAsync().Result;

                _logger.LogInformation("Publishing job run artifact '{filename}' for JobRun (ID '{jobRunId}') with '{resultLength}' bytes", contentDisposition.FileName, jobRun.Id, result.Length);
                _progressChannel.PublishArtefact(jobRun.Id, contentDisposition.FileName, result);
            }

            return StatusCode((int)HttpStatusCode.Accepted);
        }
    }
}
