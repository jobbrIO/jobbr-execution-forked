# Jobbr Forked Process Execution [![Develop build status][execution-forked-badge-build-develop]][execution-forked-link-build]
An altertative execution model that starts new processes and executes jobs in these different processes to increase stability.

[![Master build status][execution-forked-badge-build-master]][execution-forked-link-build] 
[![NuGet-Stable (Server)][execution-forked-server-badge-nuget]][execution-forked-server-link-nuget]
[![NuGet-Stable (Runtime)][execution-forked-console-badge-nuget]][execution-forked-console-link-nuget]<br/> 
[![Develop build status][execution-forked-badge-build-develop]][execution-forked-link-build] 
[![NuGet Pre-Release][execution-forked-server-badge-nuget-pre]][execution-forked-server-link-nuget] 
[![NuGet Pre-Release][execution-forked-console-badge-nuget-pre]][execution-forked-console-link-nuget]

Jobbr supports multiple execution models as described **[here]**. The forked execition model is split on the execution extension on the Jobbr-Server and it's counterpart which needs to be included in a simple ConsoleApplication, often referenced as Runner. Please see the **[wiki:concept]** or more details.

## Server Extension
It's assumed that the Jobbr-Server is already installed. The following sample shows the registration of the forked execution mode.

### NuGet
Install the NuGet `Jobbr.Execution.Forked` to the project where you host you Jobbr-Server. The extension contains everything that is needed to offload jobs to a forked process.

	Install-Package Jobbr.Execution.Forked


### Registration
```c#
using Jobbr.Exeuction.Forked;

// ....

// Create a new builder which helps to setup your JobbrServer
var builder = new JobbrBuilder();

jobbrBuilder.AddForkedExecution(config =>
    {
        // Define the executable which runs the jobs. 
        config.JobRunnerExecutable = "Runner/Demo.JobRunner.exe";
    }
);

// Create a new instance of the JobbrServer
var server = builder.Create();

// Start
server.Start();
```

### Configuration
There are additional configuration options beside the required one above.

| Name | Description | Default |
| ---- | ----------- | ------- |
|`JobRunnerExecutable` | Path to the executable that hosts a Jobbr Runtime and executes your jobs | **Required** |
|`JobRunDirectory` | Specifies the folder in which the processes should start (subfolder will be created for each run based on the id) | **Required** |
|`BackendAddress` | Defines the URI Prefix to which the forked process should connect to. <br>**Note:** Should be set manually for production scenarios. to avoid firewall issues.  | Auto |
|`MaxConcurrentProcesses` | Maximum cuncurrent executables that should be forked at a time. Usually correlates with number of Cores | 4 |
|`IsRuntimeWaitingForDebugger` | If set to true, the executable will wait up to 10s before start (or a dabugger is detected), so that a debugger can be attached to the forked process | `false` |
|`AddJobRunnerArguments` | Callback to pass additional parameters to the executable | `null` |
> **Note**: A more detailed explanation can be found the the **[wiki:configuration]**

## Runner Installation
Creating a runner and setting up all the required dependencies for the runner is your job, a short reminder: The forked execution model bases on an additional executable that executes your job. This additional executable is not part of this package and is under your control. You'll need to reference the related Job-Types in this application. The only additional thing you need to do, is to include the NuGet Package `Jobbr.Runtime.Console` in you application and start it.

    Install-Package Jobbr.Runtime.Console

In your Main() method, make sure you instantiate a new JobbrRuntime and pass all command line arguments to the Start()-Method.

```c#
using Demo.MyJobs;
using Jobbr.ConsoleApp.Runtime.Logging;
using Jobbr.Runtime.Console;

// ...

public static void Main(string[] args)
{
    // Redirect Log-Output to Trace, remove this if you install any other Log-Framework
    LogProvider.SetCurrentLogProvider(new TraceLogProvider());

    // Make sure the compiler does not remove the binding to this assembly
    var jobAssemblyToQueryJobs = typeof(ProgressJob).Assembly;

    // Set the default assembly to query for jobtypes
    var runtime = new JobbrRuntime(jobAssemblyToQueryJobs);

    // Pass the arguments of the forked execution to the runtime
    runtime.Run(args);
}
```

There is also the possibility to register your own dependency resolver which is then used to activate your Jobs. See below

```c#
// Use your own DI to activate jobs
var runtime = new JobbrRuntime(jobAssemblyToQueryJobs, new CustomDependencyResolver());
```

# License
This software is licenced under GPLv3. See [LICENSE](LICENSE) and the related licences of 3rd party libraries below.

# Acknowledgements
Jobbr Server is based on the following awesome libraries:
* [CommandLineParser](https://github.com/gsscoder/commandline) [(MIT)](https://github.com/gsscoder/commandline/blob/master/License.md)
* [LibLog](https://github.com/damianh/LibLog) [(MIT)](https://github.com/damianh/LibLog/blob/master/licence.txt)
* [Microsoft.AspNet.WebApi.Client](https://www.asp.net/web-api) [(MS .NET Library Eula)](https://www.microsoft.com/web/webpi/eula/net_library_eula_enu.htm)
* [Microsoft.AspNet.WebApi.Core](https://www.asp.net/web-api) [(MS .NET Library Eula)](https://www.microsoft.com/web/webpi/eula/net_library_eula_enu.htm)
* [Microsoft.AspNet.WebApi.Owin](https://www.asp.net/web-api) [(MS .NET Library Eula)](https://www.microsoft.com/web/webpi/eula/net_library_eula_enu.htm)
* [Microsoft.Owin](https://github.com/aspnet/AspNetKatana/) [(MS .NET Library Eula)](https://www.microsoft.com/web/webpi/eula/net_library_eula_enu.htm)
* [Microsoft.Owin.Host.HttpListener](https://github.com/aspnet/AspNetKatana/) [(MS .NET Library Eula)](https://www.microsoft.com/web/webpi/eula/net_library_eula_enu.htm)
* [Microsoft.Owin.Hosting](https://github.com/aspnet/AspNetKatana/) [(MS .NET Library Eula)](https://www.microsoft.com/web/webpi/eula/net_library_eula_enu.htm) 
* [Newtonsoft Json.NET](https://github.com/JamesNK/Newtonsoft.Json) [(MIT)](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)
* [Owin](https://github.com/owin-contrib/owin-hosting) [(Apache-2.0)](https://github.com/owin-contrib/owin-hosting/blob/master/LICENSE.txt)

# Credits
This extension was built by the following awesome developers:
* Michael Schnyder
* Oliver Zürcher
* Mark Odermatt

[execution-forked-link-build]:                  https://ci.appveyor.com/project/Jobbr/jobbr-execution-forked         
[execution-forked-server-link-nuget]:           https://www.nuget.org/packages/Jobbr.Execution.Forked
[execution-forked-console-link-nuget]:          https://www.nuget.org/packages/Jobbr.Runtime.Console

[execution-forked-badge-build-develop]:         https://img.shields.io/appveyor/ci/Jobbr/jobbr-execution-forked/develop.svg?label=develop
[execution-forked-badge-build-master]:          https://img.shields.io/appveyor/ci/Jobbr/jobbr-execution-forked/master.svg?label=master
[execution-forked-server-badge-nuget]:          https://img.shields.io/nuget/v/Jobbr.Execution.Forked.svg?label=NuGet%20stable%20%28Extension%29
[execution-forked-server-badge-nuget-pre]:      https://img.shields.io/nuget/vpre/Jobbr.Execution.Forked.svg?label=NuGet%20pre%20%28Extension%29
[execution-forked-console-badge-nuget]:         https://img.shields.io/nuget/v/Jobbr.Runtime.Console.svg?label=NuGet%20stable%20%28Runtime%29
[execution-forked-console-badge-nuget-pre]:     https://img.shields.io/nuget/vpre/Jobbr.Runtime.Console.svg?label=NuGet%20pre%20%28Runtime%29