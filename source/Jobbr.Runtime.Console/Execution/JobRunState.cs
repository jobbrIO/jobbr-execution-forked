namespace Jobbr.Runtime.Console.Execution
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