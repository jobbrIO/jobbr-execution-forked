using System;
using System.Diagnostics;
using Jobbr.ComponentModel.Execution.Model;

namespace Jobbr.Server.ForkedExecution.Core
{
    #pragma warning disable CA1001 // Types that own disposable fields should be disposable
    internal class JobRunEndedEventArgs : EventArgs
    #pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        public int ExitCode { get; set; }

        public JobRunInfo JobRun { get; set; }

        public Process ProcInfo { get; set; }
    }
}