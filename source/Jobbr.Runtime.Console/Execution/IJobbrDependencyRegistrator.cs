namespace Jobbr.Runtime.Console.Execution
{
    public interface IJobbrDependencyRegistrator : IJobbrDependencyResolver
    {
        void RegisterInstance<T>(T instance);
    }
}