using System;
using Jobbr.ComponentModel.Registration;
using Jobbr.Server.ForkedExecution.Logging;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Hosting.Services;
using Microsoft.Owin.Hosting.Starter;

namespace Jobbr.Server.ForkedExecution.BackChannel
{
    public class BackChannelWebHost : IJobbrComponent
    {
        private static readonly ILog Logger = LogProvider.For<BackChannelWebHost>();

        private readonly IJobbrServiceProvider jobbrServiceProvider;
        private readonly ForkedExecutionConfiguration configuration;

        private IDisposable webHost;

        public BackChannelWebHost(IJobbrServiceProvider jobbrServiceProvider, ForkedExecutionConfiguration configuration)
        {
            Logger.Debug("Constructing new BackChannelWebHost");

            this.jobbrServiceProvider = jobbrServiceProvider;
            this.configuration = configuration;
        }

        public string BackendAddress { get; private set; }

        public void Dispose()
        {
        }

        public void Start()
        {
            var services = (ServiceProvider)ServicesFactory.Create();
            var options = new StartOptions()
            {
                Urls = { this.configuration.BackendAddress },
                AppStartup = typeof(Startup).FullName
            };

            // Pass through the IJobbrServiceProvider to allow Startup-Classes to let them inject this dependency to owin components
            // See: http://servercoredump.com/question/27246240/inject-current-user-owin-host-web-api-service for details
            services.Add(typeof(IJobbrServiceProvider), () => this.jobbrServiceProvider);

            var hostingStarter = services.GetService<IHostingStarter>();
            this.webHost = hostingStarter.Start(options);

            Logger.InfoFormat($"Started OWIN-Host for Backchannel at '{this.configuration.BackendAddress}'.");
            this.BackendAddress = this.configuration.BackendAddress;
        }

        public void Stop()
        {
            this.webHost.Dispose();
        }
    }
}
