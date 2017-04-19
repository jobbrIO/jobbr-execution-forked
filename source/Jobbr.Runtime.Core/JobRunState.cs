namespace Jobbr.Runtime.Core
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