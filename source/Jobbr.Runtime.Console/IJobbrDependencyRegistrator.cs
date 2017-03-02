namespace Jobbr.Runtime.Console
{
    public interface IJobbrDependencyRegistrator : IJobbrDependencyResolver
    {
        void RegisterInstance<T>(T instance);
    }
}