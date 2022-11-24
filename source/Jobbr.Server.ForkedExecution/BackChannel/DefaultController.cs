using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jobbr.Server.ForkedExecution.BackChannel
{
    /// <summary>
    /// The default controller.
    /// </summary>
    [ApiController]
    [Route("fex")]
    public class DefaultController : ControllerBase
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultController"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        public DefaultController(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DefaultController>();
        }

        [HttpGet]
        [Route("status")]
        public IActionResult GetStatus()
        {
            _logger.LogDebug("Called StatusRoute");
            return Ok("All fine!");
        }
    }
}
