using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly Dictionary<long, List<JobRunStates>> jobRunStatusUpdates = new Dictionary<long, List<JobRunStates>>();

        private readonly Dictionary<long, List<double>> jobRunProgressUpdates = new Dictionary<long, List<double>>();

        private readonly Dictionary<long, List<string>> jobRunArtefactUploads = new Dictionary<long, List<string>>();

        private readonly Dictionary<Func<Dictionary<long, List<JobRunStates>>, bool>, AutoResetEvent> statusUpdateWaitCallBacks = new Dictionary<Func<Dictionary<long, List<JobRunStates>>, bool>, AutoResetEvent>();

        private readonly Dictionary<Func<Dictionary<long, List<double>>, bool>, AutoResetEvent> progressUpdateWaitCallBacks = new Dictionary<Func<Dictionary<long, List<double>>, bool>, AutoResetEvent>();

        public Dictionary<long, List<JobRunStates>> AllStatusUpdates => this.jobRunStatusUpdates;

        public Dictionary<long, List<double>> AllProgressUpdates => this.jobRunProgressUpdates;

        public Dictionary<long, List<string>> AllUploadedArtefacts => this.jobRunArtefactUploads;

        public void PublishStatusUpdate(JobRunInfo jobRunInfo, JobRunStates state)
        {
            if (!this.jobRunStatusUpdates.ContainsKey(jobRunInfo.Id))
            {
                this.jobRunStatusUpdates.Add(jobRunInfo.Id, new List<JobRunStates>());
            }

            this.jobRunStatusUpdates[jobRunInfo.Id].Add(state);

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

        public bool WaitForStatusUpdate(Func<Dictionary<long, List<JobRunStates>>, bool> allUpdates, int millisecondsTimeout)
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

        public bool WaitForProgressUpdate(Func<Dictionary<long, List<double>>, bool> allUpdates, int millisecondsTimeout)
        {
            try
            {
                var alreadyTrue = allUpdates(this.jobRunProgressUpdates);
                if (alreadyTrue)
                {
                    return true;
                }
            }
            catch { }

            var are = new AutoResetEvent(false);

            this.progressUpdateWaitCallBacks.Add(allUpdates, are);

            var successful = are.WaitOne(millisecondsTimeout, false);
            this.progressUpdateWaitCallBacks.Remove(allUpdates);

            return successful;
        }

        public void PublishProgressUpdate(JobRunInfo jobRunInfo, double progress)
        {
            if (!this.jobRunProgressUpdates.ContainsKey(jobRunInfo.Id))
            {
                this.jobRunProgressUpdates.Add(jobRunInfo.Id, new List<double>());
            }

            this.jobRunProgressUpdates[jobRunInfo.Id].Add(progress);
        }

        public void PublishArtefact(long uniqueId, string fileName, Stream result)
        {
            if (!this.jobRunArtefactUploads.ContainsKey(uniqueId))
            {
                this.jobRunArtefactUploads.Add(uniqueId, new List<string>());
            }

            this.jobRunArtefactUploads[uniqueId].Add(fileName);
        }
    }
}