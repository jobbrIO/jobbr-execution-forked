using System.Reflection;
using Jobbr.Runtime.Core;
using Jobbr.Runtime.ForkedExecution;

namespace Jobbr.Server.ForkedExecution.TestRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var runtime = new ForkedRuntime(new RuntimeConfiguration() { JobTypeSearchAssembly = Assembly.GetEntryAssembly()});

            runtime.Run(args);
        }
    }
}
