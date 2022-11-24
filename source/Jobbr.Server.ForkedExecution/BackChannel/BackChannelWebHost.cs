using System;
using System.Net;
using System.Reflection;
using Jobbr.ComponentModel.Registration;
using Microsoft.Extensions.Logging;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Hosting.Services;
using Microsoft.Owin.Hosting.Starter;

namespace Jobbr.Server.ForkedExecution.BackChannel
{
    public class BackChannelWebHost : IJobbrComponent
    {
        private readonly ILogger _logger;

        private readonly IJobbrServiceProvider _jobbrServiceProvider;
        private readonly ForkedExecutionConfiguration _configuration;

        private IDisposable _webHost;

        public BackChannelWebHost(ILoggerFactory loggerFactory, IJobbrServiceProvider jobbrServiceProvider, ForkedExecutionConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<BackChannelWebHost>();
            _logger.LogDebug("Constructing new BackChannelWebHost");

            _jobbrServiceProvider = jobbrServiceProvider;
            _configuration = configuration;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            if (_webHost != null)
            {
                throw new InvalidOperationException("The server has already been started.");
            }

            var services = (ServiceProvider)ServicesFactory.Create();
            var options = new StartOptions()
            {
                Urls = { _configuration.BackendAddress },
                AppStartup = typeof(Startup).FullName
            };

            // Pass through the IJobbrServiceProvider to allow Startup-Classes to let them inject this dependency to owin components
            // See: http://servercoredump.com/question/27246240/inject-current-user-owin-host-web-api-service for details
            services.Add(typeof(IJobbrServiceProvider), () => _jobbrServiceProvider);

            var hostingStarter = services.GetService<IHostingStarter>();

            try
            {
                _webHost = hostingStarter.Start(options);
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException is HttpListenerException)
                {
                    throw e.InnerException;
                }

                throw;
            }

            _logger.LogInformation("Started OWIN-Host for Backchannel at '{backendAddress}'.", _configuration.BackendAddress);
        }

        public void Stop()
        {
            _webHost.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (_webHost != null)
                {
                    _webHost.Dispose();
                }
            }
        }
    }
}
