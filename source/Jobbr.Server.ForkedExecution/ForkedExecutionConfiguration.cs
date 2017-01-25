using System;
using System.Collections.Generic;

namespace Jobbr.Server.ForkedExecution
{
    public class ForkedExecutionConfiguration
    {
        public string BackendAddress { get; set; }

        public int MaxConcurrentJobs { get; set; } = 4;

        public Func<string> JobRunnerExeResolver { get; set; }

        public string JobRunDirectory { get; set; }

        public bool IsRuntimeWaitingForDebugger { get; set; }

        /// <summary>
        /// Callback to add custom parameters to jobrunner
        /// </summary>
        public Func<string, string, IEnumerable<KeyValuePair<string, string>>> CustomJobRunnerParameters { get; set; }
    }
}
