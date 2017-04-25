namespace Jobbr.Runtime.ForkedExecution
{
    public enum JobRunState
    {
        Connected,
        Failed,
        Initializing,
        Processing,
        Collecting,
        Completed,
        Finishing
    }
}