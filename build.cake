#tool nuget:?package=NUnit.ConsoleRunner&version=3.12.0
#tool nuget:?package=CSharpAsyncGenerator.CommandLine&version=0.19.1
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var netfx = Argument("netfx", "net461");

//////////////////////////////////////////////////////////////////////
// CONSTANTS
//////////////////////////////////////////////////////////////////////

var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var PACKAGE_DIR = PROJECT_DIR + "package/";

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDirs = new List<string>()
{
    Directory("./NHibernate.Extensions/bin") + Directory(configuration),
    Directory("./NHibernate.Extensions.Tests/bin") + Directory(configuration)
};

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    foreach(var buildDir in buildDirs)
    {
        CleanDirectory(buildDir);
    }
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("./NHibernate.Extensions.sln");
});

Task("RestoreCore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreRestore("./NHibernate.Extensions.sln");
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    MSBuild("./NHibernate.Extensions.sln", settings =>
        settings.SetConfiguration(configuration));
});

Task("BuildCore")
    .IsDependentOn("RestoreCore")
    .Does(() =>
{
    DotNetCoreBuild("./NHibernate.Extensions.sln", new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        ArgumentCustomization = args => args.Append("--no-restore"),
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3("./NHibernate.Extensions.Tests/bin/" + configuration + $"/{netfx}/*.Tests.dll", new NUnit3Settings
    {
        NoResults = true
    });
});

Task("TestCore")
    .IsDependentOn("BuildCore")
    .Does(() =>
{
    DotNetCoreTest("./NHibernate.Extensions.Tests/NHibernate.Extensions.Tests.csproj", new DotNetCoreTestSettings
    {
        Configuration = configuration,
        NoBuild = true
    });
});

//////////////////////////////////////////////////////////////////////
// PACKAGE
//////////////////////////////////////////////////////////////////////

Task("CleanPackages")
    .Does(() =>
{
    CleanDirectory(PACKAGE_DIR);
});

Task("Pack")
    .IsDependentOn("BuildCore")
    .IsDependentOn("CleanPackages")
    .Description("Creates NuGet packages")
    .Does(() =>
{
    CreateDirectory(PACKAGE_DIR);

    var projects = new string[]
    {
        "NHibernate.Extensions/NHibernate.Extensions.csproj"
    };

    foreach(var project in projects)
    {
        MSBuild(project, new MSBuildSettings {
            Configuration = configuration,
            ArgumentCustomization = args => args
                .Append("/t:pack")
                .Append("/p:PackageOutputPath=\"" + PACKAGE_DIR + "\"")
        });
    }
});

Task("Async")
    .IsDependentOn("Restore")
    .Does(() =>
{
    DotNetCoreExecute("./Tools/CSharpAsyncGenerator.CommandLine.0.19.1/tools/net472/AsyncGenerator.CommandLine.dll");
});
    
Task("Publish")
    .IsDependentOn("Pack")
    .Does(() =>
{
    foreach(var package in System.IO.Directory.GetFiles(PACKAGE_DIR, "*.nupkg").Where(o => !o.Contains("symbols")))
    {
        NuGetPush(package, new NuGetPushSettings()
        {
            Source = "https://api.nuget.org/v3/index.json"
        });
    }
});



//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Test");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
