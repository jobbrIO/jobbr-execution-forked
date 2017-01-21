using System;
using System.Collections.Generic;
using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Execution.Model;

namespace Jobbr.Server.ForkedExecution
{
    public class ForkedJobExecutor : IJobExecutor
    {
        private readonly IJobRunInformationService jobRunInformationService;
        private readonly BackChannelWebHost backChannelWebHost;

        public ForkedJobExecutor(IJobRunInformationService jobRunInformationService, BackChannelWebHost backChannelWebHost)
        {
            this.jobRunInformationService = jobRunInformationService;
            this.backChannelWebHost = backChannelWebHost;
        }

        public void Dispose()
        {
            
        }

        public void Start()
        {

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