using System;

namespace Jobbr.Runtime.Core
{
    /// <summary>
    /// The no dependency resolver.
    /// </summary>
    public class DefaultActivator : IJobActivator
    {
        public object Activate(Type serviceType)
        {
            return Activator.CreateInstance(serviceType);
        }
    }
}