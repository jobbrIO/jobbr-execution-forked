using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Execution.Model;
using Jobbr.Server.ForkedExecution.Logging;

namespace Jobbr.Server.ForkedExecution.Core
{
    public class ForkedJobExecutor : IJobExecutor
    {
        private readonly IJobRunInformationService jobRunInformationService;
        private readonly ForkedExecutionConfiguration configuration;

        private static readonly ILog Logger = LogProvider.For<ForkedJobExecutor>();

        public ForkedJobExecutor(IJobRunInformationService jobRunInformationService, ForkedExecutionConfiguration configuration)
        {
            this.jobRunInformationService = jobRunInformationService;
            this.configuration = configuration;
        }

        public void Dispose()
        {
            
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                await Task.Delay(900);
                Logger.Debug($"Starting ForkedJobExecutor. Backchannel will be available @ '{this.configuration.BackendAddress}'");
            });
        }

        public void Stop()
        {

        }

        public void OnPlanChanged(List<PlannedJobRun> newPlan)
        {
            
        }

        public bool OnJobRunCanceled(Guid uniqueId)
        {
            return true;
        }
    }
}