using System;
using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Registration;
using Jobbr.Server.ForkedExecution.BackChannel;
using Jobbr.Server.ForkedExecution.Execution;

namespace Jobbr.Server.ForkedExecution
{
    public static class JobbrBuilderExtensions
    {
        public static void AddForkedExecution(this IJobbrBuilder builder)
        {
            AddForkedExecution(builder, configuration => { });
        }

        public static void AddForkedExecution(this IJobbrBuilder builder, Action<ForkedExecutionConfiguration> config)
        {
            var defaultConfig = new ForkedExecutionConfiguration();

            config(defaultConfig);

            builder.Add<ForkedExecutionConfiguration>(defaultConfig);

            builder.Add<IPeriodicTimer>(new FixedIntervalTimer());

            builder.Register<IJobbrComponent>(typeof(BackChannelWebHost));
            builder.Register<IJobExecutor>(typeof(ForkedJobExecutor));

            builder.Register<IConfigurationValidator>(typeof(ConfigurationValidator));
        }
    }
}
