using System.Reflection;
using Jobbr.Runtime.Console;
using Jobbr.Runtime.Core;

namespace Jobbr.Server.ForkedExecution.TestRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var runtime = new JobbrRuntime(new RuntimeConfiguration() { JobTypeSearchAssembly = Assembly.GetEntryAssembly()});

            runtime.Run(args);
        }
    }
}
