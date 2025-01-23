using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MinVer;

using static Nuke.Common.Tools.DotNet.DotNetTasks;

[DotNetVerbosityMapping]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);

    [Solution(GenerateProjects = true)] readonly Solution Solution;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    
    [Parameter("Nuget API key", Name = "api-key")] 
    readonly string NugetApiKey;
    
    [Parameter("NuGet Source for Packages", Name = "nuget-source")]
    readonly string NugetSource = "https://api.nuget.org/v3/index.json";

    [MinVer] 
    readonly MinVer MinVer;

    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath PackageDirectory => OutputDirectory / "packages";
    AbsolutePath SourceDirectory => RootDirectory / "src";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("*/bin", "*/obj").DeleteDirectories();
            OutputDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore();
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetVersion(MinVer.Version)
                .SetAssemblyVersion(MinVer.AssemblyVersion)
                .SetFileVersion(MinVer.FileVersion)
                .EnableNoRestore());
        });

    Target Tests => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .EnableNoRestore());
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .After(Tests)
        .Executes(() =>
        {
            DotNetPack(s => s
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(PackageDirectory)
                .SetVersion(MinVer.Version)
                .SetAssemblyVersion(MinVer.AssemblyVersion)
                .SetFileVersion(MinVer.FileVersion)
                .EnableNoBuild()
                .EnableNoRestore());
        });

    Target Publish => _ => _
        .DependsOn(Pack)
        .Consumes(Pack)
        .Requires(() => NugetApiKey)
        .Requires(() => Configuration.Equals(Configuration.Release))
        .Executes(() =>
        {
            DotNetNuGetPush(s => s
                .SetSource(NugetSource)
                .SetApiKey(NugetApiKey)
                .EnableSkipDuplicate()
                .CombineWith(
                    PackageDirectory.GlobFiles("*.nupkg", "*.snupkg"),
                    (_, v) => _.SetTargetPath(v)));
        });
}