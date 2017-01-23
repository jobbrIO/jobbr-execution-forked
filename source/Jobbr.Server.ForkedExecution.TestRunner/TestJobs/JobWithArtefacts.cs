using System.IO;

namespace Jobbr.Server.ForkedExecution.TestRunner.TestJobs
{
    public class JobWithArtefacts
    {
        public void Run()
        {
            File.AppendAllText("testfile.txt", "Testcontent");
        }
    }
}
