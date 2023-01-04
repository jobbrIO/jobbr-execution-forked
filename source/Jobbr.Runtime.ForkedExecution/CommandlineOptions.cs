﻿using CommandLine;

namespace Jobbr.Runtime.ForkedExecution
{
    /// <summary>
    /// The options.
    /// </summary>
    public class CommandlineOptions
    {
        /// <summary>
        /// Gets or sets the job run id.
        /// </summary>
        [Option('j', "jobRunId", Required = true)]
        public long JobRunId { get; set; }

        /// <summary>
        /// Gets or sets the job server.
        /// </summary>
        [Option('s', "server", Required = true)]
        public string JobServer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is debug.
        /// </summary>
        [Option('d', "debug", Default = false)]
        public bool IsDebug { get; set; }
    }
}