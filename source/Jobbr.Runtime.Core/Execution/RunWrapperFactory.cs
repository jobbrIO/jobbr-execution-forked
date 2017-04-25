using System;
using System.Linq;
using Jobbr.Runtime.Core.Logging;
using Newtonsoft.Json;

namespace Jobbr.Runtime.Core.Execution
{
    internal class RunWrapperFactory
    {
        private static readonly ILog Logger = LogProvider.For<RunWrapperFactory>();

        private readonly Type jobType;
        private readonly object jobParameter;
        private readonly object instanceParameter;

        public RunWrapperFactory(Type jobType, object jobParameter, object instanceParameter)
        {
            this.jobType = jobType;
            this.jobParameter = jobParameter;
            this.instanceParameter = instanceParameter;
        }

        internal object GetCastedParameterValue(string parameterName, Type targetType, string jobbrParamName, object value)
        {
            object castedValue;

            Logger.Info($"Casting {jobbrParamName}-parameter to its target value '{targetType}' based on the Run()-Parameter {parameterName}");

            // Try to cast them to specific types
            if (value == null)
            {
                Logger.Debug($"The {jobbrParamName}-parameter is null - no cast needed.");
                castedValue = null;
            }
            else if (targetType == typeof(object))
            {
                Logger.Debug($"The {jobbrParamName}-parameter is of type 'object' - no cast needed.");
                castedValue = value;
            }
            else
            {
                Logger.Debug(string.Format("The {0}-parameter '{1}' is from type '{2}'. Casting this value to '{2}'", jobbrParamName, parameterName, targetType));
                castedValue = JsonConvert.DeserializeObject(value.ToString(), targetType);
            }

            return castedValue;
        }

        internal JobWrapper CreateWrapper(object jobClassInstance)
        {
            var runMethods = this.jobType.GetMethods().Where(m => string.Equals(m.Name, "Run", StringComparison.Ordinal) && m.IsPublic).ToList();

            if (!runMethods.Any())
            {
                Logger.Error("Unable to find an entrypoint to call your job. Is there at least a public Run()-Method?");
                return null;
            }

            Action runMethodWrapper = null;

            // Try to use the method with 2 concrete parameters
            var parameterizedMethod = runMethods.FirstOrDefault(m => m.GetParameters().Length == 2);
            if (parameterizedMethod != null)
            {
                var jobParamValue = this.jobParameter ?? "<null>";
                var instanceParamValue = this.instanceParameter ?? "<null>";

                var jobParamJsonString = jobParamValue.ToString();
                var instanceParamJsonString = instanceParamValue.ToString();

                // Note: We cannot use string interpolation here, because LibLog is using string.format again and will fail if there are { } chars in the string, even if there is no formatting needed.
                Logger.DebugFormat($"Decided to use parameterized method '{parameterizedMethod}' with JobParameter '{0}' and InstanceParameters '{1}'.", jobParamJsonString, instanceParamJsonString);
                var allParams = parameterizedMethod.GetParameters().OrderBy(p => p.Position).ToList();

                var param1Type = allParams[0].ParameterType;
                var param2Type = allParams[1].ParameterType;

                var param1Name = allParams[0].Name;
                var param2Name = allParams[1].Name;

                // Casting in the most preferrable type
                var jobParameterValue = this.GetCastedParameterValue(param1Name, param1Type, "job", this.jobParameter);
                var instanceParamaterValue = this.GetCastedParameterValue(param2Name, param2Type, "instance", this.instanceParameter);

                runMethodWrapper = () => { parameterizedMethod.Invoke(jobClassInstance, new[] {jobParameterValue, instanceParamaterValue}); };
            }
            else
            {
                var fallBackMethod = runMethods.FirstOrDefault(m => !m.GetParameters().Any());

                if (fallBackMethod != null)
                {
                    Logger.Debug($"Decided to use parameterless method '{fallBackMethod}'");
                    runMethodWrapper = () => fallBackMethod.Invoke(jobClassInstance, null);
                }
            }

            if (runMethodWrapper == null)
            {
                Logger.Error("None of your Run()-Methods are compatible with Jobbr. Please see documentation");
                return null;
            }

            Logger.Debug("Initializing task for JobRun");

            return new JobWrapper(runMethodWrapper);
        }
    }
}