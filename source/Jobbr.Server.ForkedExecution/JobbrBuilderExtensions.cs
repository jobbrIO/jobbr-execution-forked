using System;
using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Registration;
using Jobbr.Server.Builder;
using Jobbr.Server.Common;
using Jobbr.Server.ForkedExecution.BackChannel;

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

            builder.Register<IJobbrComponent>(typeof(BackChannelWebHost));
            builder.Register<IJobExecutor>(typeof(ForkedJobExecutor));
        }
    }
}
