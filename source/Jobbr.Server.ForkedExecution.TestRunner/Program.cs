using System.Reflection;
using Jobbr.ConsoleApp.Runtime;

namespace Jobbr.Server.ForkedExecution.TestRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var runtime = new JobbrRuntime(Assembly.GetEntryAssembly());

            runtime.Run(args);
        }
    }
}
