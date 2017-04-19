using System;

namespace Jobbr.Runtime.Console.Execution
{
    /// <summary>
    /// The no dependency resolver.
    /// </summary>
    public class DefaultActivator : IJobActivator
    {
        public object GetService(Type serviceType)
        {
            return Activator.CreateInstance(serviceType);
        }
    }
}