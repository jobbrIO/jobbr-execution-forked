using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Execution.Model;
using Jobbr.Server.ForkedExecution.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    using JobRunStatusCallbackMatcher = Func<Dictionary<Guid, List<JobRunStates>>, bool>;

    public class GeneratedJobRunsStore
    {
        private readonly List<FakeJobRun> store = new List<FakeJobRun>();

        private readonly object syncRoot = new object();

        public FakeJobRun CreateFakeJobRun()
        {
            long id;
            lock (this.syncRoot)
            {
                id = this.store.Any() ? this.store.Max(e => e.Id) : 1;
            }

            var uniqueId = Guid.NewGuid();
            var fakeJobRun = new FakeJobRun
            {
                Id = id,
                UniqueId = uniqueId,
                PlannedJobRun = new PlannedJobRun
                {
                    PlannedStartDateTimeUtc = DateTime.UtcNow,
                    UniqueId = uniqueId
                },
                JobRunInfo = new JobRunInfo()
                {
                    UniqueId = uniqueId,
                    Id = id,
                    JobId = new Random().Next(1, Int32.MaxValue),
                    TriggerId = new Random().Next(1, Int32.MaxValue),
                }
            };

            lock (this.syncRoot)
            {
                this.store.Add(fakeJobRun);
            }

            return fakeJobRun;
        }

        public FakeJobRun GetByJobRunId(long id)
        {
            lock (this.syncRoot)
            {
                return this.store.Single(e => e.Id == id);
            }
        }

        public FakeJobRun GetByUniqueUid(Guid uniqueId)
        {
            lock (this.syncRoot)
            {
                return this.store.Single(e => e.UniqueId == uniqueId);
            }
        }
    }

    public class FakeJobRun
    {
        public Guid UniqueId { get; set; }

        public long Id { get; set; }

        public JobRunInfo JobRunInfo { get; set; }

        public PlannedJobRun PlannedJobRun { get; set; }
    }

    /// <summary>
    /// Acts as a IJobRunProgressChannel but stores all updates internally, with the option to wait for Given conditions to happen.
    /// </summary>
    public class JobRunProgressUpdateStore : IJobRunProgressChannel
    {
        private readonly Dictionary<Guid, List<JobRunStates>> jobRunStatusUpdates = new Dictionary<Guid, List<JobRunStates>>();

        private readonly Dictionary<Guid, List<double>> jobRunProgressUpdates = new Dictionary<Guid, List<double>>();

        private readonly Dictionary<JobRunStatusCallbackMatcher, AutoResetEvent> statusUpdateWaitCallBacks = new Dictionary<JobRunStatusCallbackMatcher, AutoResetEvent>();

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
                var callback = kvp.Key;

                var callbackHasPermitted = callback(this.jobRunStatusUpdates);

                if (callbackHasPermitted)
                {
                    kvp.Value.Set();
                }
            }
        }

        public bool WaitForStatusUpdate(JobRunStatusCallbackMatcher allUpdates, int millisecondsTimeout)
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

    /// <summary>
    ///  Wraps the InMemory Testing store to be able to work as a IJobRunInformationService
    /// </summary>
    public class JobRunInfoServiceMock : IJobRunInformationService
    {
        private readonly GeneratedJobRunsStore fakeStore;

        public JobRunInfoServiceMock(GeneratedJobRunsStore fakeStore)
        {
            this.fakeStore = fakeStore;
        }

        public JobRunInfo GetByUniqueId(Guid uniqueId)
        {
            return this.fakeStore.GetByUniqueUid(uniqueId).JobRunInfo;
        }
    }

    [TestClass]
    public class JobExecutorTests
    {
        [TestMethod]
        public void Empty_GetsNewPlanWithOneJob_ExecutesJob()
        {
            var store = new GeneratedJobRunsStore();
            var createdOne = store.CreateFakeJobRun();

            var progressUpdatesStore = new JobRunProgressUpdateStore();
            var infoServiceMock = new JobRunInfoServiceMock(store);

            var forkedExecutionConfiguration = new ForkedExecutionConfiguration()
            {
                BackendAddress = "any",
                JobRunDirectory = Path.GetTempPath(),
                JobRunnerExeResolver = () => "Jobbr.Server.ForkedExecution.TestEcho.exe"
            };

            var executor = new ForkedJobExecutor(infoServiceMock, forkedExecutionConfiguration, progressUpdatesStore);

            // Act
            executor.Start();
            Task.Run(() => executor.OnPlanChanged(new List<PlannedJobRun>(new [] { createdOne.PlannedJobRun, })));

            // Wait
            progressUpdatesStore.WaitForStatusUpdate(allUpdates => allUpdates[createdOne.UniqueId].Count == 2, 100000);

            // Test
            var allStatesForJob = progressUpdatesStore.AllStatusUpdates[createdOne.UniqueId];

            Assert.AreEqual(2, allStatesForJob.Count, "There should be two transitions instead of a timeout");
            Assert.AreEqual(JobRunStates.Started, allStatesForJob[1], "The last state should be 'Started' even if the process has exited sucesfully because the runtime needs to set the 'Complete'-State");
        }

        public void Empty_GetsNewPlanWith5Jobs_ExecutesMaxConfiguredJobs()
        {
            
        }
    }
}
