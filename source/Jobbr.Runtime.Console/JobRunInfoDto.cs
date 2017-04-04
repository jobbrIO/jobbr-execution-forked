using System;

namespace Jobbr.Runtime.Console
{
    /// <summary>
    /// The job run configuration.
    /// </summary>
    public class JobRunInfoDto
    {
        public long JobId { get; set; }

        public long TriggerId { get; set; }

        public string UserId { get; set; }

        public string UserDisplayName { get; set; }

        public long JobRunId { get; set; }

        public Guid UniqueId { get; set; }

        public object JobParameter { get; set; }

        public object InstanceParameter { get; set; }

        public string JobName { get; set; }

        public string JobType { get; set; }

        public string WorkingDir { get; set; }

        public string TempDir { get; set; }
    }
}
