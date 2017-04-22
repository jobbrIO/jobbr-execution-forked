using System;

namespace Jobbr.Runtime.Core
{
    /// <summary>
    /// The no dependency resolver.
    /// </summary>
    public class DefaultServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            return Activator.CreateInstance(serviceType);
        }
    }
}