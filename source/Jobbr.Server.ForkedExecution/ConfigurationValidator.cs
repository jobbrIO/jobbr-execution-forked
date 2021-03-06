﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Jobbr.ComponentModel.Registration;
using Jobbr.Server.ForkedExecution.Logging;

namespace Jobbr.Server.ForkedExecution
{
    public class ConfigurationValidator : IConfigurationValidator
    {
        private static readonly ILog Logger = LogProvider.For<ConfigurationValidator>();

        public Type ConfigurationType { get; set; } = typeof(ForkedExecutionConfiguration);

        public bool Validate(object configuration)
        {
            var config = (ForkedExecutionConfiguration)configuration;

            if (config == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.BackendAddress))
            {
                // Fallback to automatic endpoint port
                Logger.Warn("There was no BackendAdress specified. Falling back to random port, which is not guaranteed to work in production scenarios");
                var port = NextFreeTcpPort();

                config.BackendAddress = $"http://localhost:{port}";
            }

            if (string.IsNullOrEmpty(config.JobRunDirectory))
            {
                throw new ArgumentException("Please provide a JobRunDirectory!");
            }

            var jobRunDirectoryPath = Path.GetFullPath(config.JobRunDirectory);
            if (!Directory.Exists(jobRunDirectoryPath))
            {
                throw new ArgumentException($"The Directory '{jobRunDirectoryPath}' cannot be found!");
            }

            if (string.IsNullOrWhiteSpace(config.JobRunnerExecutable))
            {
                throw new ArgumentException("You should set a runner-Executable which runs your jobs later!");
            }

            var executableFullPath = Path.GetFullPath(config.JobRunnerExecutable);

            if (!File.Exists(executableFullPath))
            {
                throw new ArgumentException($"The RunnerExecutable '{executableFullPath}' cannot be found!");
            }

            return true;
        }

        private static int NextFreeTcpPort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}
