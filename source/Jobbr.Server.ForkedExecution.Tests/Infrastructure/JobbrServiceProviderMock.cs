using System;
using System.Collections.Generic;
using Jobbr.ComponentModel.Registration;
using Jobbr.Server.ForkedExecution.BackChannel;

namespace Jobbr.Server.ForkedExecution.Tests.Infrastructure
{
    public class JobbrServiceProviderMock : IJobbrServiceProvider
    {
        private readonly JobRunInfoServiceMock jobRunInformationService;
        private readonly ProgressChannelStore progressChannelStore;

        public JobbrServiceProviderMock(JobRunInfoServiceMock jobRunInformationService, ProgressChannelStore progressChannelStore)
        {
            this.jobRunInformationService = jobRunInformationService;
            this.progressChannelStore = progressChannelStore;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(ForkedExecutionController))
            {
                return new ForkedExecutionController(this.jobRunInformationService, this.progressChannelStore);
            }

            return null;
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return new List<object>();
        }

        public IJobbrServiceProvider GetChild()
        {
            return this;
        }
    }
}