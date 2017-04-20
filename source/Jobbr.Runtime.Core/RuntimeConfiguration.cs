using System.Reflection;

namespace Jobbr.Runtime.Core
{
    public class RuntimeConfiguration
    {
        public Assembly JobTypeSearchAssembly { get; set; }

        public IJobActivator JobActivator { get; set; }

        public RuntimeConfiguration()
        {
            this.JobActivator = new DefaultActivator();
        }
    }
}