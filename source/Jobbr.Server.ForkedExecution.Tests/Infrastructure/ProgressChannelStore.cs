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
    public class ProgressChannelStore : IJobRunProgressChannel
    {
        private readonly Dictionary<long, List<JobRunStates>> jobRunStatusUpdates = new Dictionary<long, List<JobRunStates>>();

        private readonly Dictionary<long, List<double>> jobRunProgressUpdates = new Dictionary<long, List<double>>();

        private readonly Dictionary<long, List<string>> jobRunArtefactUploads = new Dictionary<long, List<string>>();

        private readonly Dictionary<long, List<Tuple<string, long>>> jobRunPids = new Dictionary<long, List<Tuple<string, long>>>();

        private readonly Dictionary<Func<Dictionary<long, List<JobRunStates>>, bool>, AutoResetEvent> statusUpdateWaitCallBacks = new Dictionary<Func<Dictionary<long, List<JobRunStates>>, bool>, AutoResetEvent>();

        private readonly Dictionary<Func<Dictionary<long, List<double>>, bool>, AutoResetEvent> progressUpdateWaitCallBacks = new Dictionary<Func<Dictionary<long, List<double>>, bool>, AutoResetEvent>();

        public Dictionary<long, List<JobRunStates>> AllStatusUpdates => this.jobRunStatusUpdates;

        public Dictionary<long, List<double>> AllProgressUpdates => this.jobRunProgressUpdates;

        public Dictionary<long, List<string>> AllUploadedArtefacts => this.jobRunArtefactUploads;

        public Dictionary<long, List<Tuple<string, long>>> AllPids => this.jobRunPids;

        public void PublishStatusUpdate(long jobRunId, JobRunStates state)
        {
            if (!this.jobRunStatusUpdates.ContainsKey(jobRunId))
            {
                this.jobRunStatusUpdates.Add(jobRunId, new List<JobRunStates>());
            }

            this.jobRunStatusUpdates[jobRunId].Add(state);

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

        public bool WaitForStatusUpdate(Func<Dictionary<long, List<JobRunStates>>, bool> updatesFromAllJobs, int millisecondsTimeout)
        {
            try
            {
                var alreadyTrue = updatesFromAllJobs(this.jobRunStatusUpdates);
                if (alreadyTrue)
                {
                    return true;
                }
            }
            catch
            {
            }

            var are = new AutoResetEvent(false);

            this.statusUpdateWaitCallBacks.Add(updatesFromAllJobs, are);

            var successful = are.WaitOne(millisecondsTimeout, false);
            this.statusUpdateWaitCallBacks.Remove(updatesFromAllJobs);

            return successful;
        }

        public bool WaitForProgressUpdate(Func<Dictionary<long, List<double>>, bool> updatesFromAllJobs, int millisecondsTimeout)
        {
            try
            {
                var alreadyTrue = updatesFromAllJobs(this.jobRunProgressUpdates);
                if (alreadyTrue)
                {
                    return true;
                }
            }
            catch
            {
            }

            var are = new AutoResetEvent(false);

            this.progressUpdateWaitCallBacks.Add(updatesFromAllJobs, are);

            var successful = are.WaitOne(millisecondsTimeout, false);
            this.progressUpdateWaitCallBacks.Remove(updatesFromAllJobs);

            return successful;
        }

        public void PublishProgressUpdate(long jobRunId, double progress)
        {
            if (!this.jobRunProgressUpdates.ContainsKey(jobRunId))
            {
                this.jobRunProgressUpdates.Add(jobRunId, new List<double>());
            }

            this.jobRunProgressUpdates[jobRunId].Add(progress);
        }

        public void PublishArtefact(long uniqueId, string fileName, Stream result)
        {
            if (!this.jobRunArtefactUploads.ContainsKey(uniqueId))
            {
                this.jobRunArtefactUploads.Add(uniqueId, new List<string>());
            }

            this.jobRunArtefactUploads[uniqueId].Add(fileName);
        }

        public void PublishPid(long jobRunId, int pid, string host)
        {
            if (!this.jobRunPids.ContainsKey(jobRunId))
            {
                this.jobRunPids.Add(jobRunId, new List<Tuple<string, long>>());
            }

            this.jobRunPids[jobRunId].Add(new Tuple<string, long>(host, pid));
        }
    }
}