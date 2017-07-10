using System.Reflection;
using Jobbr.DevSupport.ReferencedVersionAsserter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class PackagingTests
    {
        private readonly Asserter featureAsserter = new Asserter(Asserter.ResolvePackagesConfig("Jobbr.Server.ForkedExecution"), Asserter.ResolveRootFile("Jobbr.Execution.Forked.nuspec"));
        private readonly Asserter runtimeAssetrer = new Asserter(Asserter.ResolvePackagesConfig("Jobbr.Runtime.ForkedExecution"), Asserter.ResolveRootFile("Jobbr.Runtime.ForkedExecution.nuspec"));

        private readonly bool isPre = Assembly.GetExecutingAssembly().GetInformalVersion().Contains("-");

        [TestMethod]
        public void ExecutionFeature_KnownReferences_AreDeclared()
        {
            this.featureAsserter.Add(new PackageExistsInBothRule("Jobbr.ComponentModel.Registration"));
            this.featureAsserter.Add(new PackageExistsInBothRule("Jobbr.ComponentModel.Execution"));

            var result = this.featureAsserter.Validate();

            Assert.IsTrue(result.IsSuccessful, result.Message);
        }

        [TestMethod]
        public void ExecutionFeature_PreComponentModelsPre_ExactSameVersions()
        {
            if (!this.isPre)
            {
                // This rule is only valid for Pre-Release versions because we only need exact match on PreRelease Versions
                return;
            }

            this.featureAsserter.Add(new ExactVersionMatchRule("Jobbr.ComponentModel.*"));

            var result = this.featureAsserter.Validate();

            Assert.IsTrue(result.IsSuccessful, result.Message);
        }

        [TestMethod]
        public void ExecutionFeature_ComponentModels_InRange()
        {
            this.featureAsserter.Add(new VersionIsIncludedInRange("Jobbr.ComponentModel.*"));

            var result = this.featureAsserter.Validate();

            Assert.IsTrue(result.IsSuccessful, result.Message);
        }

        [TestMethod]
        public void ExecutionFeature_AllDependencies_NoMajorVersionChangeAllowed()
        {
            this.featureAsserter.Add(new NoMajorChangesInNuSpec("Jobbr.*"));
            this.featureAsserter.Add(new NoMajorChangesInNuSpec("Microsoft.*"));

            var result = this.featureAsserter.Validate();

            Assert.IsTrue(result.IsSuccessful, result.Message);
        }

        [TestMethod]
        public void Runtime_KnownReferences_AreNone()
        {
            this.runtimeAssetrer.Add(new NoExternalDependenciesRule());

            var result = this.featureAsserter.Validate();

            Assert.IsTrue(result.IsSuccessful, result.Message);
        }
    }
}
