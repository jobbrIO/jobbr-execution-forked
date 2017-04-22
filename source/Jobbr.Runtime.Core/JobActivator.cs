using System;
using Jobbr.Runtime.Core.Logging;

namespace Jobbr.Runtime.Core
{
    public class JobActivator
    {
        private static readonly ILog Logger = LogProvider.For<JobActivator>();

        private readonly JobTypeResolver jobTypeResolver;
        private readonly IServiceProvider serviceProvider;

        public JobActivator(JobTypeResolver jobTypeResolver, IServiceProvider serviceProvider)
        {
            this.jobTypeResolver = jobTypeResolver;
            this.serviceProvider = serviceProvider;
        }

        public object CreateInstance(string jobTypeName)
        {
            // Resolve Type
            Logger.Debug($"Trying to resolve the specified type '{jobTypeName}'...");

            var type = this.jobTypeResolver.ResolveType(jobTypeName);

            if (type == null)
            {
                Logger.Error($"Unable to resolve the type '{jobTypeName}'!");
                return null;
            }

            // Activation
            Logger.Debug($"Type '{jobTypeName}' has been resolved to '{type}'. Activating now.");

            object jobClassInstance;

            try
            {
                jobClassInstance = this.serviceProvider.GetService(type);
            }
            catch (Exception exception)
            {
                Logger.ErrorException($"Exception while activating type '{type}'. See Exception for details!", exception, type);
                return null;
            }

            if (jobClassInstance == null)
            {
                Logger.Error($"Unable to create an instance ot the type '{type}'!");
            }

            return jobClassInstance;
        }

        public void RegisterDependencies(params object[] additionalDependencies)
        {
            var registrator = this.serviceProvider as IConfigurableServiceProvider;

            try
            {
                foreach (var dep in additionalDependencies)
                {
                    registrator?.RegisterInstance(dep);
                }

            }
            catch (Exception e)
            {
                Logger.WarnException($"Unable to register additional dependencies on {registrator}!", e);
            }
        }
    }
}