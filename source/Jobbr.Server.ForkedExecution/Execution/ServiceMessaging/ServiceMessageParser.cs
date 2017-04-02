using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Jobbr.Server.ForkedExecution.Execution.ServiceMessaging
{
    public class ServiceMessageParser
    {
        private static readonly IEnumerable<Type> KnownTypes;

        static ServiceMessageParser()
        {
            KnownTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes());
        }

        public ServiceMessage Parse(string serviceMessage)
        {
            var messageTypeRaw = string.Empty;
            var parametersRaw = string.Empty;

            Regex regex = new Regex(@"##jobbr\[([a-z]*[A-Z]*) (.*)\]");

            foreach (Match match in regex.Matches(serviceMessage))
            {
                GroupCollection collection = match.Groups;

                if (collection.Count == 3)
                {
                    messageTypeRaw = collection[1].Value;
                    parametersRaw = collection[2].Value;
                }
            }

            // Identity CLR-MessageType
            var typeNameLowerCase = messageTypeRaw + "servicemessage";

            var messageTypes = KnownTypes.Where(t => t.IsSubclassOf(typeof(ServiceMessage)));
            var type = messageTypes.FirstOrDefault(t => string.Equals(t.Name.ToLowerInvariant(), typeNameLowerCase, StringComparison.Ordinal));

            if (type == null)
            {
                return null;
            }

            // Identity Parameters
            var splitted = parametersRaw.Split(new[] { '\'', '=' }, StringSplitOptions.RemoveEmptyEntries);
            var parameters = new Dictionary<string, string>();

            for (int i = 0; i < splitted.Length - 1; i = i + 2)
            {
                parameters.Add(splitted[i], splitted[i + 1]);
            }

            var instance = (ServiceMessage)Activator.CreateInstance(type);

            foreach (var key in parameters.Keys)
            {
                var prop = type.GetProperties().FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));

                if (prop != null)
                {
                    if (prop.PropertyType == typeof(double))
                    {
                        prop.SetValue(instance, double.Parse(parameters[key]));
                    }

                    if (prop.PropertyType == typeof(int))
                    {
                        prop.SetValue(instance, int.Parse(parameters[key]));
                    }

                    if (prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(instance, parameters[key]);
                    }
                }
            }

            return instance;
        }
    }
}