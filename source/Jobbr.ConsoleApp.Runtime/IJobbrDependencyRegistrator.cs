namespace Jobbr.ConsoleApp.Runtime
{
    public interface IJobbrDependencyRegistrator : IJobbrDependencyResolver
    {
        void RegisterInstance<T>(T instance);
    }
}