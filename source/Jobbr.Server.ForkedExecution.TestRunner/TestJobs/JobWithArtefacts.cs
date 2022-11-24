using System.IO;

namespace Jobbr.Server.ForkedExecution.TestRunner.TestJobs
{
    public static class JobWithArtefacts
    {
        public static void Run()
        {
            File.AppendAllText("testfile.txt", "Testcontent");
        }
    }
}
