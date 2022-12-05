using Jobbr.Runtime.ForkedExecution;
using Jobbr.Runtime;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobbr.Server.ForkedExecution.TestRunner
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var runtime = new ForkedRuntime(new NullLoggerFactory(), new RuntimeConfiguration { JobTypeSearchAssemblies = new[] { Assembly.GetEntryAssembly() } });

            runtime.Run(args);
        }
    }
}
