using System;
using System.Reflection;
using Jobbr.Runtime.Core.Activation;

namespace Jobbr.Runtime.Core
{
    public class RuntimeConfiguration
    {
        public Assembly JobTypeSearchAssembly { get; set; }

        public IServiceProvider ServiceProvider { get; set; }

        public RuntimeConfiguration()
        {
            this.ServiceProvider = new DefaultServiceProvider();
        }
    }
}