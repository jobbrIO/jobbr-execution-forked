using System;
using System.Collections.Generic;
using System.Linq;
using Jobbr.ComponentModel.Execution.Model;

namespace Jobbr.Server.ForkedExecution.Tests.Infrastructure
{
    public class FakeGeneratedJobRunsStore
    {
        private readonly List<FakeJobRunStoreTuple> store = new List<FakeJobRunStoreTuple>();

        private readonly object syncRoot = new object();

        public FakeJobRunStoreTuple CreateFakeJobRun()
        {
            long id;
            lock (this.syncRoot)
            {
                id = this.store.Any() ? this.store.Max(e => e.Id) : 1;
            }

            var uniqueId = Guid.NewGuid();
            var fakeJobRun = new FakeJobRunStoreTuple
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

        public FakeJobRunStoreTuple GetByJobRunId(long id)
        {
            lock (this.syncRoot)
            {
                return this.store.Single(e => e.Id == id);
            }
        }

        public FakeJobRunStoreTuple GetByUniqueUid(Guid uniqueId)
        {
            lock (this.syncRoot)
            {
                return this.store.Single(e => e.UniqueId == uniqueId);
            }
        }
    }
}