using System;

namespace Jobbr.Server.ForkedExecution.Execution
{
    public interface IPeriodicTimer
    {
        void Setup(Action value, long intervalInSeconds);

        void Start();

        void Stop();
    }
}