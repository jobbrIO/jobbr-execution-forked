namespace Jobbr.Runtime.Console.Execution
{
    public interface IJobbrDependencyRegistrator : IJobActivator
    {
        void RegisterInstance<T>(T instance);
    }
}