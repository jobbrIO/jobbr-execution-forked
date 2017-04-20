namespace Jobbr.Runtime.Core
{
    public class JobRunInfo
    {
        public object JobParameter { get; set; }

        public object InstanceParameter { get; set; }

        public string JobType { get; set; }

        public string UserId { get; set; }

        public string UserDisplayName { get; set; }
    }
}