using System.IO;

namespace Jobbr.Server.ForkedExecution.TestEcho
{
    class Program
    {
        static void Main(string[] args)
        {
            var filename = Path.GetRandomFileName();
            File.WriteAllText(filename, string.Join("\n", args));
        }
    }
}
