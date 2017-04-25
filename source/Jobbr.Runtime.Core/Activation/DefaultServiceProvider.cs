using System;

namespace Jobbr.Runtime.Core.Activation
{
    /// <summary>
    /// The default service provider is able to activate jobtypes without any dependencies
    /// </summary>
    internal class DefaultServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            return Activator.CreateInstance(serviceType);
        }
    }
}