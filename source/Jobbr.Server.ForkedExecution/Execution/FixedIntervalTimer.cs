using System;
using System.Threading;

namespace Jobbr.Server.ForkedExecution.Execution
{
    internal class FixedIntervalTimer : IPeriodicTimer
    {
        private Action callback;

        private readonly Timer timer;
        private TimeSpan interval;

        public FixedIntervalTimer()
        {
            this.timer = new Timer(state => this.callback());
        }

        public void Setup(Action value, long intervalInSeconds)
        {
            this.callback = value;
            this.interval = TimeSpan.FromSeconds(intervalInSeconds);
        }

        public void Start()
        {
            this.timer.Change(this.interval, this.interval);
        }

        public void Stop()
        {
            this.timer.Change(int.MaxValue, int.MaxValue);
        }
    }
}