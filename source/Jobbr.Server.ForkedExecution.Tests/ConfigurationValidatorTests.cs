using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class ConfigurationValidatorTests
    {
        private readonly ConfigurationValidator _configurationValidator;

        public ConfigurationValidatorTests()
        {
            var loggerFactory = new LoggerFactory();
            _configurationValidator = new ConfigurationValidator(loggerFactory);
        }

        private static ForkedExecutionConfiguration GetWorkingConfiguration()
        {
            return new ForkedExecutionConfiguration
            {
                JobRunnerExecutable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"),
                BackendAddress = "http://localhost:1234",
                JobRunDirectory = Directory.GetCurrentDirectory(),
                MaxConcurrentProcesses = 4
            };
        }

        [TestMethod]
        public void WorkingConfiguration_Validated_IsFine()
        {
            var config = GetWorkingConfiguration();

            var validationResult = _configurationValidator.Validate(config);

            Assert.IsTrue(validationResult);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RunnerExecutable_IsNull_ValidationThrowsException()
        {
            var config = GetWorkingConfiguration();

            config.JobRunnerExecutable = null;

            _configurationValidator.Validate(config);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RunnerExecutable_Empty_ValidationThrowsException()
        {
            var config = GetWorkingConfiguration();

            config.JobRunnerExecutable = string.Empty;

            _configurationValidator.Validate(config);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RunnerExecutable_IsInvalidPath_ValidationThrowsException()
        {
            var config = GetWorkingConfiguration();

            config.JobRunnerExecutable = "C:\\bla\\blupp.exe";

            _configurationValidator.Validate(config);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void JobRunDirectory_IsNull_ValidationThrowsException()
        {
            var config = GetWorkingConfiguration();

            config.JobRunDirectory = null;

            _configurationValidator.Validate(config);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void JobRunDirectory_Empty_ValidationThrowsException()
        {
            var config = GetWorkingConfiguration();

            config.JobRunDirectory = string.Empty;

            _configurationValidator.Validate(config);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void JobRunDirectory_IsInvalidPath_ValidationThrowsException()
        {
            var config = GetWorkingConfiguration();

            config.JobRunDirectory = "C:\\bla\\";

            _configurationValidator.Validate(config);
        }
    }
}
