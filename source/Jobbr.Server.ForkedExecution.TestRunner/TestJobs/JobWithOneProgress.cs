using System;

namespace Jobbr.Server.ForkedExecution.TestRunner.TestJobs
{
    public class TestArguments
    {
        public bool ShouldFail { get; set; }
    }

    public static class JobWithOneProgress
    {
        public static double DefinedProgressValue = 42.135;

        public static void Run(TestArguments runParams)
        {
            Console.WriteLine($"##jobbr[progress percent='{DefinedProgressValue}']");

            if (runParams?.ShouldFail == true)
            {
                Environment.Exit(-38464);
            }
        }
    }
}
