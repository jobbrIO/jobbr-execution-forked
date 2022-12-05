using System.Reflection;
using Jobbr.DevSupport.ReferencedVersionAsserter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.ForkedExecution.Tests
{
    [TestClass]
    public class PackagingTests
    {
        private readonly bool isPre = Assembly.GetExecutingAssembly().GetInformalVersion().Contains("-");

        [TestMethod]
        public void Feature_NuSpec_IsCompliant()
        {
            var asserter = new Asserter(Asserter.ResolveProjectFile("Jobbr.Server.ForkedExecution", "Jobbr.Server.ForkedExecution.csproj"), Asserter.ResolveRootFile("Jobbr.Execution.Forked.nuspec"));

            asserter.Add(new PackageExistsInBothRule("Jobbr.ComponentModel.Registration"));
            asserter.Add(new PackageExistsInBothRule("Jobbr.ComponentModel.Execution"));
            asserter.Add(new VersionIsIncludedInRange("Jobbr.ComponentModel.*"));
            asserter.Add(new NoMajorChangesInNuSpec("Jobbr.*"));

            var result = asserter.Validate();

            Assert.IsTrue(result.IsSuccessful, result.Message);
        }

        [TestMethod]
        public void Runtime_NuSpec_IsCompliant()
        {
            var asserter = new Asserter(Asserter.ResolveProjectFile("Jobbr.Runtime.ForkedExecution", "Jobbr.Runtime.ForkedExecution.csproj"), Asserter.ResolveRootFile("Jobbr.Runtime.ForkedExecution.nuspec"));
            asserter.Add(new NoExternalDependenciesRule());

            var result = asserter.Validate();

            Assert.IsTrue(result.IsSuccessful, result.Message);
        }
    }
}
