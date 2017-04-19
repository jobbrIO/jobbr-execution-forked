namespace Jobbr.Runtime.Core
{
    public interface IJobbrDependencyRegistrator : IJobActivator
    {
        void RegisterInstance<T>(T instance);
    }
}