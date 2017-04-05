using System;

namespace Jobbr.Server.ForkedExecution.TestRunner.TestJobs
{
    public class TestArguments
    {
        public bool ShouldFail { get; set; }
    }

    public class JobWithOneProgress
    {
        public void Run(object jobParams, TestArguments runParams)
        {
            Console.WriteLine("##jobbr[progress percent='42.135']");

            if (runParams.ShouldFail)
            {
                Environment.Exit(-38464);
            }
        }
    }
}
