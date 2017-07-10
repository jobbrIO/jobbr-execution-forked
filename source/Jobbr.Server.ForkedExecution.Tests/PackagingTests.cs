using Jobbr.DevSupport.ReferencedVersionAsserter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class PackagingTests
    {
        [TestMethod]
        public void ExecutionFeature_KnownkReferences_AreDeclared()
        {
            var featureAsserter = new Asserter("../../../Jobbr.Server.ForkedExecution/packages.config", "../../../Jobbr.Execution.Forked.nuspec");

            featureAsserter.Add(new PackageExistsInBothRule("Jobbr.ComponentModel.Registration"));
            featureAsserter.Add(new PackageExistsInBothRule("Jobbr.ComponentModel.Execution"));

            var result = featureAsserter.Validate();

            Assert.IsTrue(result.IsSuccessful, result.Message);
        }

        [TestMethod]
        public void ExecutionFeature_ComponentModels_SameVersions()
        {
            var featureAsserter = new Asserter("../../../Jobbr.Server.ForkedExecution/packages.config", "../../../Jobbr.Execution.Forked.nuspec");

            featureAsserter.Add(new ExactVersionMatchRule("Jobbr.ComponentModel.*"));

            var result = featureAsserter.Validate();

            Assert.IsTrue(result.IsSuccessful, result.Message);
        }

    }
}
