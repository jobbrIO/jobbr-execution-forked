using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class ConfigurationValidatorTests
    {
        private readonly ConfigurationValidator sut;

        public ConfigurationValidatorTests()
        {
            this.sut = new ConfigurationValidator();
        }

        private ForkedExecutionConfiguration GetWorkingConfiguration()
        {
            return new ForkedExecutionConfiguration()
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
            var config = this.GetWorkingConfiguration();

            var validationResult = this.sut.Validate(config);

            Assert.IsTrue(validationResult);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RunnerExecutable_IsNull_ValidationThrowsException()
        {
            var config = this.GetWorkingConfiguration();

            config.JobRunnerExecutable = null;

            this.sut.Validate(config);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RunnerExecutable_Empty_ValidationThrowsException()
        {
            var config = this.GetWorkingConfiguration();

            config.JobRunnerExecutable = string.Empty;

            this.sut.Validate(config);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RunnerExecutable_IsInvalidPath_ValidationThrowsException()
        {
            var config = this.GetWorkingConfiguration();

            config.JobRunnerExecutable = "C:\\bla\\blupp.exe";

            this.sut.Validate(config);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void JobRunDirectory_IsNull_ValidationThrowsException()
        {
            var config = this.GetWorkingConfiguration();

            config.JobRunDirectory = null;

            this.sut.Validate(config);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void JobRunDirectory_Empty_ValidationThrowsException()
        {
            var config = this.GetWorkingConfiguration();

            config.JobRunDirectory = string.Empty;

            this.sut.Validate(config);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void JobRunDirectory_IsInvalidPath_ValidationThrowsException()
        {
            var config = this.GetWorkingConfiguration();

            config.JobRunDirectory = "C:\\bla\\";

            this.sut.Validate(config);
        }
    }
}
