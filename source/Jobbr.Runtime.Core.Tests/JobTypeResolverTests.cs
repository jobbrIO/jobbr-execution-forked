using System.Reflection;
using Jobbr.Runtime.Core.Activation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Runtime.Core.Tests
{
    [TestClass]
    public class JobTypeResolverTests
    {
        public class JobInExecutingAssembly
        {
            public void Run() { }
        }

        [TestMethod]
        public void TypeFullName_SearchAssemblySet_TypeFound()
        {
            var jobTypeSearchAssembly = Assembly.GetExecutingAssembly();

            var jobType = typeof(JobInExecutingAssembly).FullName;

            var jobTypeResolver = new JobTypeResolver(jobTypeSearchAssembly);

            var result = jobTypeResolver.ResolveType(jobType);

            Assert.IsNotNull(result);
            Assert.AreEqual(typeof(JobInExecutingAssembly), result);
        }

        [TestMethod]
        public void AssemblyFullqualifiedName_NoSearchAssemblySet_TypeFound()
        {
            var jobType = typeof(JobInExecutingAssembly).AssemblyQualifiedName;

            var jobTypeResolver = new JobTypeResolver(null);

            var result = jobTypeResolver.ResolveType(jobType);

            Assert.IsNotNull(result);
            Assert.AreEqual(typeof(JobInExecutingAssembly), result);
        }
    }
}
