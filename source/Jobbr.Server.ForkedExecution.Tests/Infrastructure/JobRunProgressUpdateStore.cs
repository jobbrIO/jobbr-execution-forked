using System;
using System.Collections.Generic;
using System.Threading;
using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Execution.Model;

namespace Jobbr.Server.ForkedExecution.Tests.Infrastructure
{
    /// <summary>
    /// Acts as a IJobRunProgressChannel but stores all updates internally, with the option to wait for Given conditions to happen.
    /// </summary>
    public class JobRunProgressUpdateStore : IJobRunProgressChannel
    {
        private readonly Dictionary<Guid, List<JobRunStates>> jobRunStatusUpdates = new Dictionary<Guid, List<JobRunStates>>();

        private readonly Dictionary<Guid, List<double>> jobRunProgressUpdates = new Dictionary<Guid, List<double>>();

        private readonly Dictionary<Func<Dictionary<Guid, List<JobRunStates>>, bool>, AutoResetEvent> statusUpdateWaitCallBacks = new Dictionary<Func<Dictionary<Guid, List<JobRunStates>>, bool>, AutoResetEvent>();

        public Dictionary<Guid, List<JobRunStates>> AllStatusUpdates => this.jobRunStatusUpdates;

        public void PublishStatusUpdate(JobRunInfo jobRunInfo, JobRunStates state)
        {
            if (!this.jobRunStatusUpdates.ContainsKey(jobRunInfo.UniqueId))
            {
                this.jobRunStatusUpdates.Add(jobRunInfo.UniqueId, new List<JobRunStates>());
            }

            this.jobRunStatusUpdates[jobRunInfo.UniqueId].Add(state);

            foreach (var kvp in this.statusUpdateWaitCallBacks)
            {
                try
                {
                    var callback = kvp.Key;

                    var callbackHasPermitted = callback(this.jobRunStatusUpdates);

                    if (callbackHasPermitted)
                    {
                        kvp.Value.Set();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public bool WaitForStatusUpdate(Func<Dictionary<Guid, List<JobRunStates>>, bool> allUpdates, int millisecondsTimeout)
        {
            try
            {
                var alreadyTrue = allUpdates(this.jobRunStatusUpdates);
                if (alreadyTrue)
                {
                    return true;
                }
            }
            catch { }

            var are = new AutoResetEvent(false);

            this.statusUpdateWaitCallBacks.Add(allUpdates, are);

            var successful = are.WaitOne(millisecondsTimeout, false);
            this.statusUpdateWaitCallBacks.Remove(allUpdates);

            return successful;
        }

        public void PublishProgressUpdate(JobRunInfo jobRunInfo, double progress)
        {
            if (!this.jobRunProgressUpdates.ContainsKey(jobRunInfo.UniqueId))
            {
                this.jobRunProgressUpdates.Add(jobRunInfo.UniqueId, new List<double>());
            }

            this.jobRunProgressUpdates[jobRunInfo.UniqueId].Add(progress);
        }
    }
}