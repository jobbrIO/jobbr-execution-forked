using System;
using System.Threading.Tasks;
using Jobbr.ComponentModel.Registration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobbr.Server.ForkedExecution.BackChannel
{
    /// <summary>
    /// Creates and starts BackChannel web host.
    /// </summary>
    public class BackChannelWebHost : IJobbrComponent
    {
        private readonly ILogger _logger;
        private readonly IJobbrServiceProvider _jobbrServiceProvider;
        private readonly ForkedExecutionConfiguration _configuration;
        private IWebHost _webHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackChannelWebHost"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="jobbrServiceProvider">Service provider.</param>
        /// <param name="configuration">Forked execution configuration.</param>
        public BackChannelWebHost(ILoggerFactory loggerFactory, IJobbrServiceProvider jobbrServiceProvider, ForkedExecutionConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<BackChannelWebHost>();
            _logger.LogDebug("Constructing new BackChannelWebHost");

            _jobbrServiceProvider = jobbrServiceProvider;
            _configuration = configuration;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Task.FromResult(_webHost.StopAsync());
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Start the web host.
        /// </summary>
        /// <exception cref="InvalidOperationException">The host is already running.</exception>
        public void Start()
        {
            if (_webHost != null)
            {
                throw new InvalidOperationException("The server has already been started.");
            }

            _webHost = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(_configuration.BackendAddress)
                .ConfigureServices(services =>
                {
                    services.AddSingleton(_jobbrServiceProvider);
                })
                .UseStartup<Startup>()
                .Build();

            _webHost.Start();

            _logger.LogInformation("Started web host for Backchannel at '{backendAddress}'.", _configuration.BackendAddress);
        }

        /// <summary>
        /// Stop the hosting by disposing the host object.
        /// </summary>
        public void Stop()
        {
            Task.FromResult(_webHost.StopAsync());
        }

        /// <summary>
        /// Conditional web host dispose.
        /// </summary>
        /// <param name="disposing">If true, dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Task.FromResult(_webHost.StopAsync());
                _webHost?.Dispose();
            }
        }
    }
}
