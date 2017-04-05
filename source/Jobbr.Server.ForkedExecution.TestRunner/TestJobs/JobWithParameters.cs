namespace Jobbr.Server.ForkedExecution.TestRunner.TestJobs
{
    public class JobWithParameters
    {
        public void Run(JobParams jobParams, RunParams runParams)
        {
            
        }
    }

    public class RunParams
    {
        public string Param1 { get; set; }
    }

    public class JobParams
    {
        public string Param2 { get; set; }
    }
}
