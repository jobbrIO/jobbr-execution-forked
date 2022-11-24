using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using Microsoft.Extensions.Logging;

namespace Jobbr.Server.ForkedExecution.BackChannel
{
    public class TraceSourceExceptionLogger : IExceptionLogger
    {
        private readonly ILogger _logger;

        public TraceSourceExceptionLogger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TraceSourceExceptionLogger>();
        }

        public Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            var logAsync = new Task(() => _logger.LogError(context.Exception, "Unhandled Exception while processing request '{request}'", context.Request));

            logAsync.Start();

            return logAsync;
        }
    }
}