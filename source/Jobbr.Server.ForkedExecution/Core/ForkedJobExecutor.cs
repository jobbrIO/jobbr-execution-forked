using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Execution.Model;
using Jobbr.Server.ForkedExecution.Logging;

namespace Jobbr.Server.ForkedExecution.Core
{
    public class ForkedJobExecutor : IJobExecutor
    {
        private static readonly ILog Logger = LogProvider.For<ForkedJobExecutor>();

        private static long MaxWebHostStartWait = 2000;

        private static readonly int StartNewJobsEverySeconds = 1;

        private readonly IJobRunInformationService jobRunInformationService;
        private readonly ForkedExecutionConfiguration configuration;
        private readonly IJobRunProgressChannel progressChannel;

        private readonly List<PlannedJobRun> plannedJobRuns = new List<PlannedJobRun>();

        private List<JobRunContext> activeContexts = new List<JobRunContext>();

        private readonly Timer timer;
        private readonly object syncRoot = new object();


        public ForkedJobExecutor(IJobRunInformationService jobRunInformationService, IJobRunProgressChannel progressChannel, ForkedExecutionConfiguration configuration)
        {
            this.jobRunInformationService = jobRunInformationService;
            this.configuration = configuration;
            this.progressChannel = progressChannel;

            this.timer = new Timer(this.StartReadyJobsFromQueue, null, Timeout.Infinite, Timeout.Infinite);
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

                if (string.IsNullOrWhiteSpace(this.configuration.BackendAddress) || duration.ElapsedMilliseconds > MaxWebHostStartWait)
                {
                    throw new Exception("Unable to information about the BackChannel Connection readyness. Executor will be in unhealthy state");
                }

                Logger.Debug($"ForkedJobExecutor started. Backchannel will be available @ '{this.configuration.BackendAddress}'");

                Logger.InfoFormat("Enabling periodic check for JobRuns to start every {0}s", StartNewJobsEverySeconds);
                this.timer.Change(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(StartNewJobsEverySeconds));
            });
        }

        public void Stop()
        {
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void OnPlanChanged(List<PlannedJobRun> newPlan)
        {
            var hadChanges = 0;

            lock (this.syncRoot)
            {
                // Load all existing Schedules into the local memory
                if (!newPlan.Any())
                {
                    return;
                }

                Logger.InfoFormat("Got a plan with {0} scheduled JobRuns with an upcoming startdate", newPlan.Count);

                // Add only new
                var toAdd = newPlan.Where(newItem => this.plannedJobRuns.All(existingItem => existingItem.UniqueId != newItem.UniqueId)).ToList();
                this.plannedJobRuns.AddRange(toAdd);
                hadChanges += toAdd.Count;

                Logger.InfoFormat("Added {0} new planned jobruns based on the new plan", toAdd.Count);

                // Remove non existing
                var toRemove = this.plannedJobRuns.Where(existingItem => newPlan.All(newItem => existingItem.UniqueId != newItem.UniqueId)).ToList();
                this.plannedJobRuns.RemoveAll(p => toRemove.Contains(p));
                hadChanges += toRemove.Count;

                // Update startdates of existing
                // TODO

                Logger.InfoFormat("Removed {0} previously planned jobruns.", toRemove.Count);
            }

            if (hadChanges > 0)
            {
                // Immediately handle changes
                this.timer.Change(TimeSpan.FromMilliseconds(0), TimeSpan.FromSeconds(StartNewJobsEverySeconds));
            }
        }

        public bool OnJobRunCanceled(Guid uniqueId)
        {
            throw new NotImplementedException();
        }

        private void StartReadyJobsFromQueue(object state)
        {
            lock (this.syncRoot)
            {
                var possibleJobsToStart = this.configuration.MaxConcurrentJobs - this.activeContexts.Count;
                var readyJobs = this.plannedJobRuns.Where(jr => jr.PlannedStartDateTimeUtc <= DateTime.UtcNow).OrderBy(jr => jr.PlannedStartDateTimeUtc).ToList();

                var jobsToStart = readyJobs.Take(possibleJobsToStart).ToList();

                var queueCannotStartAll = readyJobs.Count > possibleJobsToStart;
                var showStatusInformationNow = (DateTime.Now.Second % 5) == 0;
                var canStartAllReadyJobs = jobsToStart.Count > 0 && jobsToStart.Count <= possibleJobsToStart;

                if ((queueCannotStartAll && showStatusInformationNow) || canStartAllReadyJobs)
                {
                    Logger.InfoFormat("There are {0} planned jobs in the queue and currently {1} running jobs. Number of possible jobs to start: {2}", readyJobs.Count, this.activeContexts.Count, possibleJobsToStart);
                }

                foreach (var jobRun in jobsToStart)
                {
                    Logger.Debug($"Trying to start job with UniqueId '{jobRun.UniqueId}' which was planned for {jobRun.PlannedStartDateTimeUtc}.");

                    try
                    {
                        Logger.Debug($"Getting Metadata for a job (UniqueId '{jobRun.UniqueId}') that needs to be started.");
                        var jobRunInfo = this.jobRunInformationService.GetByUniqueId(jobRun.UniqueId);

                        var wrapper = new JobRunContext(jobRunInfo, this.configuration, this.progressChannel);
                        this.activeContexts.Add(wrapper);
                        this.plannedJobRuns.Remove(jobRun);

                        wrapper.Start();
                        wrapper.Ended += this.ContextOnEnded;
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorException(string.Format("Exception was thrown while starting a new JobRun with UniqueId: {0}.", jobRun.UniqueId), e);
                    }
                }
            }
        }

        private void ContextOnEnded(object sender, JobRunEndedEventArgs args)
        {
            JobRunInfo run;

            lock (this.syncRoot)
            {
                var jobRunContext = sender as JobRunContext;

                jobRunContext.Ended -= this.ContextOnEnded;

                run = args.JobRun;
                Logger.InfoFormat("Removing context for JobRun with Id: {0} (TriggerId: {1}, JobId: {2})", run.Id, run.TriggerId, run.JobId);

                this.activeContexts.Remove(jobRunContext);
            }

            try
            {
                // TODO: this.stateService.SetJobRunEndTime(args.JobRun, DateTime.UtcNow);
            }
            catch (Exception e)
            {
                Logger.ErrorException(string.Format("Exception while setting the end-time of the jobRun with id: {0} (TriggerId: {1}, JobId: {2})", run.Id, run.TriggerId, run.JobId), e);
            }

            if (args.ExitCode != 0)
            {
                Logger.WarnFormat("The process within the context JobRun has exited with a non-zero exit code. JobRunId: {0} (TriggerId: {1}, JobId: {2})", run.Id, run.TriggerId, run.JobId);

                try
                {
                    this.progressChannel.PublishStatusUpdate(run, JobRunStates.Failed);
                }
                catch (Exception e)
                {
                    Logger.ErrorException(string.Format("Exception while setting the 'Failed'-State to the jobRun with id: {0} (TriggerId: {1}, JobId: {2})", run.Id, run.TriggerId, run.JobId), e);
                }
            }
            else
            {
                //if (args.JobRun.Progress > 0)
                //{
                //    try
                //    {
                //        // TODO: this.stateService.UpdateJobRunProgress(args.JobRun.Id, 100);
                //    }
                //    catch (Exception e)
                //    {
                //        Logger.ErrorException(string.Format("Exception while setting progress to 100% after completion of the jobRun with id: {0} (TriggerId: {1}, JobId: {2})", run.Id, run.TriggerId, run.JobId), e);
                //    }
                //}
            }
        }
    }
}