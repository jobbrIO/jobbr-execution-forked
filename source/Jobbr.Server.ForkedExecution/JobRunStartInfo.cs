namespace Jobbr.Server.ForkedExecution
{
    public class JobRunStartInfo
    {
        public long JobId { get; internal set; }

        public string UniqueName { get; internal set; }

        public string JobType { get; internal set; }

        public long JobRunId { get; internal set; }

        public long TriggerId { get; internal set; }

        public long? UserId { get; internal set; }
    }
}