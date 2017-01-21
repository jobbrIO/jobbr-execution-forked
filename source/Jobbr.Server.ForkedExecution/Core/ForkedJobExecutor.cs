using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private static long MaxWebHostStartWait = 2000;

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
            Logger.Debug($"Starting ForkedJobExecutor asynchonously...");
            var duration = new Stopwatch();
            duration.Start();

            Task.Run(async () =>
            {
                while (string.IsNullOrWhiteSpace(this.configuration.BackendAddress) && duration.ElapsedMilliseconds < MaxWebHostStartWait)
                {
                    await Task.Delay(200);
                }
                duration.Stop();

                if (!string.IsNullOrWhiteSpace(this.configuration.BackendAddress) || duration.ElapsedMilliseconds > MaxWebHostStartWait)
                {
                    throw new Exception("Unable to information about the BackChannel Connection readyness. Executor will be in unhealth state");
                }

                Logger.Debug($"ForkedJobExecutor started. Backchannel will be available @ '{this.configuration.BackendAddress}'");
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
            return false;
        }
    }
}