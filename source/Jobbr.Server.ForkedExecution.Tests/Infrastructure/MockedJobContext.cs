using System;
using Jobbr.ComponentModel.Execution;
using Jobbr.ComponentModel.Execution.Model;
using Jobbr.Server.ForkedExecution.Execution;

namespace Jobbr.Server.ForkedExecution.Tests.Infrastructure
{
    public class MockedJobContext : IJobRunContext
    {
        private readonly JobRunInfo jobRunInfo;
        private readonly IJobRunProgressChannel progressChannel;
        private bool didReportProgress;

        public MockedJobContext(JobRunInfo jobRunInfo, IJobRunProgressChannel progressChannel)
        {
            this.jobRunInfo = jobRunInfo;
            this.progressChannel = progressChannel;
        }

        public event EventHandler<JobRunEndedEventArgs> Ended;

        public long JobRunId => this.jobRunInfo.Id;

        public void Start()
        {
            this.progressChannel.PublishStatusUpdate(this.jobRunInfo.Id, JobRunStates.Starting);
        }

        public void RaiseProgressUpdate(double progress)
        {
            this.didReportProgress = true;
            this.progressChannel.PublishProgressUpdate(this.jobRunInfo.Id, progress);
        }

        public void RaiseStatusChange(JobRunStates state)
        {
            this.progressChannel.PublishStatusUpdate(this.jobRunInfo.Id, state);
        }

        public void RaiseEnded(int exitCode = 0)
        {
            this.OnEnded(new JobRunEndedEventArgs() { ExitCode = exitCode, JobRun = this.jobRunInfo, DidReportProgress = this.didReportProgress });
        }

        protected virtual void OnEnded(JobRunEndedEventArgs e)
        {
            this.Ended?.Invoke(this, e);
        }
    }
}