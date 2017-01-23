using System;
using System.Collections.Generic;
using Jobbr.ComponentModel.Registration;
using Jobbr.Server.ForkedExecution.BackChannel;

namespace Jobbr.Server.ForkedExecution.Tests.Infrastructure
{
    public class JobbrServiceProviderMock : IJobbrServiceProvider
    {
        private readonly JobRunInfoServiceMock jobRunInformationService;
        private readonly JobRunProgressUpdateStore storedProgressUpdates;

        public JobbrServiceProviderMock(JobRunInfoServiceMock jobRunInformationService, JobRunProgressUpdateStore storedProgressUpdates)
        {
            this.jobRunInformationService = jobRunInformationService;
            this.storedProgressUpdates = storedProgressUpdates;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(ForkedExecutionController))
            {
                return new ForkedExecutionController(this.jobRunInformationService, this.storedProgressUpdates);
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