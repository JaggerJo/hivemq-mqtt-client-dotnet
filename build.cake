var target = Argument("Target", "Default");
var configuration =
    HasArgument("Configuration") ? Argument<string>("Configuration") :
    EnvironmentVariable("Configuration", "Release");

var artifactsDirectory = Directory("./Artifacts");

Task("Clean")
    .Description("Cleans the artifacts, bin and obj directories.")
    .Does(() =>
    {
        CleanDirectory(artifactsDirectory);
        DeleteDirectories(GetDirectories("**/bin"), new DeleteDirectorySettings() { Force = true, Recursive = true });
        DeleteDirectories(GetDirectories("**/obj"), new DeleteDirectorySettings() { Force = true, Recursive = true });
    });

Task("Restore")
    .Description("Restores NuGet packages.")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        DotNetRestore();
    });

Task("Build")
    .Description("Builds the solution.")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        DotNetBuild(
            ".",
            new DotNetBuildSettings()
            {
                Configuration = configuration,
                NoRestore = true,
            });
    });

Task("Test")
    .Description("Runs unit tests and outputs test results to the artifacts directory.")
    .DoesForEach(
        GetFiles("./Tests/**/*.csproj"),
        project =>
        {
            DotNetTest(
                project.ToString(),
                new DotNetTestSettings()
                {
                    Blame = true,
                    Collectors = new string[] { "Code Coverage", "XPlat Code Coverage" },
                    Configuration = configuration,
                    Loggers = new string[]
                    {
                        $"trx;LogFileName={project.GetFilenameWithoutExtension()}.trx",
                        $"html;LogFileName={project.GetFilenameWithoutExtension()}.html",
                    },
                    NoBuild = true,
                    NoRestore = true,
                    ResultsDirectory = artifactsDirectory,
                });
        });

Task("Pack")
    .Description("Creates NuGet packages and outputs them to the artifacts directory.")
    .Does(() =>
    {
        DotNetPack(
            ".",
            new DotNetPackSettings()
            {
                Configuration = configuration,
                IncludeSymbols = true,
                MSBuildSettings = new DotNetMSBuildSettings()
                {
                    ContinuousIntegrationBuild = !BuildSystem.IsLocalBuild,
                },
                NoBuild = true,
                NoRestore = true,
                OutputDirectory = artifactsDirectory,
            });
    });

Task("CountLines")
    .Description("Counts the total lines of C# code in the Source folder.")
    .Does(() =>
    {
        var csFiles = GetFiles("./Source/**/*.cs");
        var totalLines = 0;
        var fileCount = 0;
        
        foreach(var file in csFiles)
        {
            var lines = System.IO.File.ReadAllLines(file.FullPath);
            totalLines += lines.Length;
            fileCount++;
        }
        
        Information("===========================================");
        Information("C# Code Statistics for Source Folder");
        Information("===========================================");
        Information($"Total C# Files: {fileCount}");
        Information($"Total Lines of Code: {totalLines}");
        Information("===========================================");
    });

Task("Default")
    .Description("Cleans, restores NuGet packages, builds the solution, runs unit tests and then creates NuGet packages.")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack");

RunTarget(target);
