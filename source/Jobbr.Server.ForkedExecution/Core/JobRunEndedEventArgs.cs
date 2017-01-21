using System;
using System.Diagnostics;
using Jobbr.ComponentModel.Execution.Model;

namespace Jobbr.Server.ForkedExecution.Core
{
    internal class JobRunEndedEventArgs : EventArgs
    {
        public int ExitCode { get; set; }

        public JobRunInfo JobRun { get; set; }

        public Process ProcInfo { get; set; }
    }
}