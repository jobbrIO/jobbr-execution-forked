using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Runtime.Core.Tests
{
    [TestClass]
    public class RuntimeContextTests
    {
        public class CustomActivator : IConfigurableServiceProvider
        {
            readonly List<object> registeredInstances = new List<object>();

            public List<object> RegisteredInstances
            {
                get { return this.registeredInstances; }
            }

            public object GetService(Type serviceType)
            {
                return null;
            }

            public void RegisterInstance<T>(T instance)
            {
                this.registeredInstances.Add(instance);
            }
        }

        public class TestJob
        {
        }

        public class RunCallBackTestJob
        {
            private static Action callback;

            private static readonly object CallBackLock = new object();

            public static Action Callback
            {
                set
                {
                    lock (CallBackLock)
                    {
                        if (callback != null)
                        {
                            Assert.Fail($"Cannot use {nameof(RunCallBackTestJob)} in more than one test simzultaneously.");
                        }

                        callback = value;
                    }
                }
            }

            public static void Reset()
            {
                lock (CallBackLock)
                {
                    callback = null;
                }
            }

            public void Run()
            {
                lock (CallBackLock)
                {
                    callback();
                }
            }
        }

        [TestMethod]
        public void ConfigurableServiceProviderIsSet_WhenExecuting_RegistrationIsCalled()
        {
            var serviceProvider = new CustomActivator();

            var runtime = new CoreRuntime(new RuntimeConfiguration { ServiceProvider = serviceProvider });
            runtime.Execute(new ExecutionMetadata { JobType = typeof(TestJob).AssemblyQualifiedName });

            #pragma warning disable CS0618 // Type or member is obsolete
            Assert.AreEqual(1, serviceProvider.RegisteredInstances.OfType<RuntimeContext>().Count(), "There should be a single registration of the RuntimeContext");
            #pragma warning restore CS0618 // Type or member is obsolete
        }

        [TestMethod]
        public void ConfigurableServiceProviderIsSet_WhenExecuting_ContextMatchesMetaInfo()
        {
            var serviceProvider = new CustomActivator();

            var userName = "michael.schnyder@zuehlke.com";
            var userDisplay = "Schnyder, Michael";

            var runtime = new CoreRuntime(new RuntimeConfiguration { ServiceProvider = serviceProvider });
            runtime.Execute(new ExecutionMetadata { JobType = typeof(TestJob).AssemblyQualifiedName, UserId = userName, UserDisplayName = userDisplay });

            #pragma warning disable CS0618 // Type or member is obsolete
            var ctx = serviceProvider.RegisteredInstances.OfType<RuntimeContext>().Single();

            Assert.AreEqual(userName, ctx.UserId);
            Assert.AreEqual(userDisplay, ctx.UserDisplayName);
            #pragma warning restore CS0618 // Type or member is obsolete
        }

        [TestMethod]
        public void CallingThreadPrincipal_WhenUserIsNotSet_DoesNotChange()
        {
            var currentThreadPrincipal = Thread.CurrentPrincipal;

            var runtime = new CoreRuntime(new RuntimeConfiguration());
            runtime.Execute(new ExecutionMetadata { JobType = typeof(TestJob).AssemblyQualifiedName });

            Assert.AreEqual(currentThreadPrincipal, Thread.CurrentPrincipal);
        }

        [TestMethod]
        public void CallingThreadPrincipal_WhenUserIsSet_DoesNotChange()
        {
            var currentThreadPrincipal = Thread.CurrentPrincipal;

            var runtime = new CoreRuntime(new RuntimeConfiguration());
            runtime.Execute(new ExecutionMetadata { JobType = typeof(TestJob).AssemblyQualifiedName, UserId = "bla"});

            Assert.AreEqual(currentThreadPrincipal, Thread.CurrentPrincipal);
        }

        [TestMethod]
        public void ExecutingThreadPrincipal_WhenExecuting_CallbackIsCalled()
        {
            IPrincipal executingThreadPrincipal = null;

            RunCallBackTestJob.Callback = () => executingThreadPrincipal = Thread.CurrentPrincipal;

            var runtime = new CoreRuntime(new RuntimeConfiguration());
            runtime.Execute(new ExecutionMetadata { JobType = typeof(RunCallBackTestJob).AssemblyQualifiedName });

            RunCallBackTestJob.Reset();

            Assert.IsNotNull(executingThreadPrincipal);
        }

        [TestMethod]
        public void ExecutingThreadPrincipal_WhenUserIsNotSet_DoesNotChange()
        {
            IPrincipal executingThreadPrincipal = null;

            RunCallBackTestJob.Callback = () => executingThreadPrincipal = Thread.CurrentPrincipal;

            var runtime = new CoreRuntime(new RuntimeConfiguration());
            runtime.Execute(new ExecutionMetadata { JobType = typeof(RunCallBackTestJob).AssemblyQualifiedName });

            RunCallBackTestJob.Reset();

            Assert.AreEqual(executingThreadPrincipal, Thread.CurrentPrincipal);
        }

        [TestMethod]
        public void ExecutingThreadPrincipal_WhenUserIsSet_DoesChangePrincipal()
        {
            IPrincipal executingThreadPrincipal = null;

            RunCallBackTestJob.Callback = () => executingThreadPrincipal = Thread.CurrentPrincipal;

            var runtime = new CoreRuntime(new RuntimeConfiguration());
            runtime.Execute(new ExecutionMetadata { JobType = typeof(RunCallBackTestJob).AssemblyQualifiedName, UserId = "anything"});

            RunCallBackTestJob.Reset();

            Assert.AreNotEqual(executingThreadPrincipal, Thread.CurrentPrincipal);
        }

        [TestMethod]
        public void ThreadPrincipal_InExecutingThread_IdentityNameContainsUserId()
        {
            IPrincipal executingThreadPrincipal = null;

            RunCallBackTestJob.Callback = () => executingThreadPrincipal = Thread.CurrentPrincipal;
            var userName = "michael.schnyder@zuehlke.com";

            var runtime = new CoreRuntime(new RuntimeConfiguration());
            runtime.Execute(new ExecutionMetadata { JobType = typeof(RunCallBackTestJob).AssemblyQualifiedName, UserId = userName});

            RunCallBackTestJob.Reset();

            Assert.AreEqual(userName, executingThreadPrincipal.Identity.Name);
        }

        [TestMethod]
        public void ThreadPrincipal_InExecutingThread_AuthenticationTypeIsSet()
        {
            IPrincipal executingThreadPrincipal = null;

            RunCallBackTestJob.Callback = () => executingThreadPrincipal = Thread.CurrentPrincipal;
            var userName = "michael.schnyder@zuehlke.com";

            var runtime = new CoreRuntime(new RuntimeConfiguration());
            runtime.Execute(new ExecutionMetadata { JobType = typeof(RunCallBackTestJob).AssemblyQualifiedName, UserId = userName });

            RunCallBackTestJob.Reset();

            Assert.AreEqual("JobbrIdentity", executingThreadPrincipal.Identity.AuthenticationType);
        }
    }
}
