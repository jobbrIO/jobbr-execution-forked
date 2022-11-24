using System;
using System.Threading;

namespace Jobbr.Server.ForkedExecution.Execution
{
    internal class FixedIntervalTimer : IPeriodicTimer, IDisposable
    {
        private Action _callback;

        private Timer _timer;
        private TimeSpan _interval;

        public FixedIntervalTimer()
        {
            _timer = new Timer(state => _callback());
        }

        ~FixedIntervalTimer()
        {
            Dispose(false);
        }

        public void Setup(Action value, long intervalInSeconds)
        {
            _callback = value;
            _interval = TimeSpan.FromSeconds(intervalInSeconds);
        }

        public void Start()
        {
            _timer.Change(_interval, _interval);
        }

        public void Stop()
        {
            _timer.Change(int.MaxValue, int.MaxValue);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }
    }
}