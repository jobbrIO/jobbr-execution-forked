using System;

namespace Jobbr.Server.ForkedExecution.TestRunner.TestJobs
{
    public class JobWithOneProgress
    {
        public void Run()
        {
            Console.WriteLine("##jobbr[progress percent='42.135']");
        }
    }
}
