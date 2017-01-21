using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Registration;
using Jobbr.Server.Builder;
using Jobbr.Server.Common;

namespace Jobbr.Server.ForkedExecution
{
    public static class JobbrBuilderExtensions
    {
        public static void AddForkedExecution(this IJobbrBuilder builder)
        {
            builder.Register<IJobbrComponent>(typeof(BackChannelWebHost));
            builder.Register<IJobExecutor>(typeof(ForkedJobExecutor));
        }
    }
}
