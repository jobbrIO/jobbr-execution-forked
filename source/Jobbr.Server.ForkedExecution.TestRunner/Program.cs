using System.Reflection;
using Jobbr.Runtime;
using Jobbr.Runtime.ForkedExecution;

namespace Jobbr.Server.ForkedExecution.TestRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var runtime = new ForkedRuntime(new RuntimeConfiguration() { JobTypeSearchAssemblies = new [] { Assembly.GetEntryAssembly() }});

            runtime.Run(args);
        }
    }
}
