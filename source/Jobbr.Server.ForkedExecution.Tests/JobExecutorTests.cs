using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Execution.Model;
using Jobbr.Server.ForkedExecution.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class JobExecutorTests
    {
        [TestMethod]
        public void Empty_GetsNewPlan_ExecutesJob()
        {
            var uniqueId = Guid.NewGuid();
            var plannedJobRun = new PlannedJobRun() { PlannedStartDateTimeUtc = DateTime.UtcNow, UniqueId = uniqueId };
            var jobRunInfo = new JobRunInfo() { UniqueId = uniqueId, JobId = 1, Id = 1, TriggerId = 1 };

            var capturedStates = new List<JobRunStates>();
            var are = new AutoResetEvent(false);

            var forkedExecutionConfiguration = new ForkedExecutionConfiguration()
            {
                BackendAddress = "any",
                JobRunDirectory = Path.GetTempPath(),
                JobRunnerExeResolver = () => "Jobbr.Server.ForkedExecution.TestEcho.exe"
            };

            var jobInformationServiceMock = new Mock<IJobRunInformationService>();
            jobInformationServiceMock.Setup(m => m.GetByUniqueId(uniqueId)).Returns(() => jobRunInfo);

            var jobRunProgressChannelMock = new Mock<IJobRunProgressChannel>();
            jobRunProgressChannelMock.Setup(m => m.PublishStatusUpdate(It.IsNotNull<JobRunInfo>(), It.IsNotNull<JobRunStates>())).Callback((JobRunInfo i, JobRunStates s) =>
            {
                capturedStates.Add(s);
                if (capturedStates.Count == 2)
                {
                    are.Reset();
                }
            });

            var executor = new ForkedJobExecutor(jobInformationServiceMock.Object, forkedExecutionConfiguration, jobRunProgressChannelMock.Object);
            executor.Start();

            executor.OnPlanChanged(new List<PlannedJobRun>(new [] { plannedJobRun, }));

            are.WaitOne(1000);

            Assert.AreEqual(2, capturedStates.Count, "There should be two transitions: 'Starting' and 'Started'");
            Assert.AreEqual(JobRunStates.Started, capturedStates[1], "The last state should be 'Started' even if the process has exited sucesfully because the runtime needs to set the 'Complete'-State");
        }
    }
}
