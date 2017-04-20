using System;

namespace Jobbr.Runtime.Core
{
    public class FinishingEventArgs
    {
        public bool Successful { get; set; }

        public Exception Exception { get; set; }
    }
}