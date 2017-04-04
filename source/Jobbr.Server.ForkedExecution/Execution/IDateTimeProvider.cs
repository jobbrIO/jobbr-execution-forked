using System;

namespace Jobbr.Server.ForkedExecution.Execution
{
    public interface IDateTimeProvider
    {
        DateTime GetUtcNow();
    }
}