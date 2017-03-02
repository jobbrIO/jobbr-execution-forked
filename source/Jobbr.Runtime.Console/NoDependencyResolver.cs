using System;
using System.Collections.Generic;

namespace Jobbr.Runtime.Console
{
    /// <summary>
    /// The no dependency resolver.
    /// </summary>
    public class NoDependencyResolver : IJobbrDependencyResolver
    {
        public object GetService(Type serviceType)
        {
            return Activator.CreateInstance(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return new List<object>(new[] { this.GetService(serviceType) });
        }
    }
}