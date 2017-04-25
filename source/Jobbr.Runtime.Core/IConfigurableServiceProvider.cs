using System;

namespace Jobbr.Runtime.Core
{
    public interface IConfigurableServiceProvider : IServiceProvider
    {
        void RegisterInstance<T>(T instance);
    }
}