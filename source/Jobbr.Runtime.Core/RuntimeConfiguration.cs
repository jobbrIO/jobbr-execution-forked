using System;
using System.Reflection;

namespace Jobbr.Runtime.Core
{
    public class RuntimeConfiguration
    {
        public Assembly JobTypeSearchAssembly { get; set; }

        public IServiceProvider ServiceProvider { get; set; }
    }
}