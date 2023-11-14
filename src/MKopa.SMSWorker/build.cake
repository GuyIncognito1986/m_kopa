var target = Argument("target", "Test");
var configuration = Argument("configuration", "Release");
var baseDir = string.Empty;
var solutionFilePath = System.IO.Path.Join(baseDir, "MKopa.SMSWorker.sln");

var projectNames = new [] {
    "MKopa.SMS.Worker",
    "MKopa.SMS.Worker.Lib"
};

var testProjectNames = new [] {
        "MKopa.SMS.Worker.UnitTests",
        "MKopa.SMS.Worker.AcceptanceTests"
};

private IEnumerable<string> BuildProjectPaths(IEnumerable<string> projNames)
{
    return projNames.Select(n => System.IO.Path.Join(baseDir, n, $"{n}.csproj"));
}

var testProjectPaths = BuildProjectPaths(testProjectNames);
var allProjectNames = projectNames.Concat(testProjectNames);
var allProjectPaths = BuildProjectPaths(allProjectNames);
var allProjectDirs = allProjectNames.Select(n => System.IO.Path.Join(baseDir, n));

Task("Clean")
    .Does(() => 
{
    var subDirectoresToClean = new [] { "bin", "obj"};
    foreach (var d in allProjectDirs)
    {
        foreach(var subd in subDirectoresToClean)
        {
            var cleanDir = System.IO.Path.Join(d, subd);
            if(System.IO.Directory.Exists(cleanDir))
            {
                DeleteDirectory(cleanDir, new DeleteDirectorySettings {
                    Recursive = true,
                    Force = true
                });
            }
        }
    }
});
    
Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetBuild(solutionFilePath, new DotNetBuildSettings
    {
        Configuration = configuration
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    foreach(var tp in testProjectPaths)
    {
        DotNetTest(tp, new DotNetTestSettings
        {
            Configuration = configuration,
            NoBuild = true,
        });
    }
});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);