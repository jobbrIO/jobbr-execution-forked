namespace Jobbr.Runtime.ForkedExecution
{
    public enum JobRunStates
    {
        Null,
        Scheduled,
        Preparing,
        Starting,
        Started,
        Connected,
        Initializing,
        Processing,
        Finishing,
        Collecting,
        Completed,
        Failed,
        Deleted,
        Omitted,
    }
}