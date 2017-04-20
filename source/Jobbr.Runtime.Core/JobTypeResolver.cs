using System;
using System.Linq;
using System.Reflection;
using Jobbr.Runtime.Core.Logging;

namespace Jobbr.Runtime.Core
{
    public class JobTypeResolver
    {
        private static readonly ILog Logger = LogProvider.For<JobTypeResolver>();

        private readonly Assembly defaultAssembly;

        public JobTypeResolver(Assembly defaultAssembly)
        {
            this.defaultAssembly = defaultAssembly;
        }

        public Type ResolveType(string typeName)
        {
            Logger.Debug($"Resolve type using '{typeName}' like a full qualified CLR-Name");
            var type = Type.GetType(typeName);

            if (type == null && this.defaultAssembly != null)
            {
                Logger.Debug($"Trying to resolve '{typeName}' by the assembly '{this.defaultAssembly.FullName}'");
                type = this.defaultAssembly.GetType(typeName);
            }

            if (type == null)
            {
                // Search in all Assemblies
                var allReferenced = Assembly.GetExecutingAssembly().GetReferencedAssemblies();

                Logger.Debug($"Trying to resolve type by asking all referenced assemblies ('{string.Join(", ", allReferenced.Select(a => a.Name))}')");

                foreach (var assemblyName in allReferenced)
                {
                    var assembly = Assembly.Load(assemblyName);

                    var foundType = assembly.GetType(typeName, false, true);

                    if (foundType != null)
                    {
                        type = foundType;
                    }
                }
            }

            if (type == null)
            {
                Logger.Debug($"Still no luck finding '{typeName}' somewhere. Iterating through all types and comparing class-names. Please hold on");

                // Absolutely no clue
                var matchingTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => string.Equals(x.Name, typeName, StringComparison.Ordinal) && x.IsClass && !x.IsAbstract).ToList();

                if (matchingTypes.Count() == 1)
                {
                    Logger.Debug($"Found matching type: '{matchingTypes[0]}'");
                    type = matchingTypes.First();
                }
                else if (matchingTypes.Count > 1)
                {
                    Logger.Warn($"More than one matching type found for '{typeName}'. Matches: {string.Join(", ", matchingTypes.Select(t => t.FullName))}");
                }
                else
                {
                    Logger.Warn($"No matching type found for '{typeName}'.");
                }
            }

            return type;
        }
    }
}