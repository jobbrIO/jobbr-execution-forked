using System.Web.Http;
using Jobbr.Server.ForkedExecution.Logging;

namespace Jobbr.Server.ForkedExecution.BackChannel
{
    [RoutePrefix("fex")]
    public class DefaultController : ApiController
    {
        private static readonly ILog Logger = LogProvider.For<DefaultController>();

        [HttpGet]
        [Route("status")]
        public IHttpActionResult GetStatus()
        {
            Logger.Debug("Called StatusRoute");
            return this.Ok("All fine!");
        }
    }
}
