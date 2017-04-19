namespace Jobbr.Runtime.Core
{
    public interface IRuntimeContextRegistrator : IJobActivator
    {
        void RegisterInstance<T>(T instance);
    }
}