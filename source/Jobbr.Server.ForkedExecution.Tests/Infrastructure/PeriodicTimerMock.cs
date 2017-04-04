using System;
using Jobbr.Server.ForkedExecution.Execution;

namespace Jobbr.Server.ForkedExecution.Tests.Infrastructure
{
    public class PeriodicTimerMock : IPeriodicTimer
    {
        private Action callback;

        public void Setup(Action value, long intervalInSeconds)
        {
            this.callback = value;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void CallbackOnce()
        {
            this.callback();
        }
    }
}