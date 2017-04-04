using System;

namespace Jobbr.Server.ForkedExecution.Execution
{
    internal class UtcNowTimeProvider : IDateTimeProvider
    {
        DateTime IDateTimeProvider.GetUtcNow() => DateTime.UtcNow;
    }
}