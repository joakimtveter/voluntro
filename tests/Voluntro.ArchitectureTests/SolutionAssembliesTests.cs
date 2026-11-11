namespace Voluntro.ArchitectureTests;

public sealed class SolutionAssembliesTests
{
    private static readonly string[] _guardedFolders = ["BuildingBlocks", "Modules"];

    [Fact]
    public void Scan_finds_every_BuildingBlocks_and_Modules_project_on_disk()
    {
        // Guards against the rules passing on nothing: if a project exists under these folders
        // but the csproj globs or the scan miss it, its rules would never run.
        var projects = GuardedProjectNames();

        Assert.NotEmpty(projects);
        Assert.Empty(projects.Except(SolutionAssemblies.Graph.Keys));
    }

    [Fact]
    public void Scan_leaves_out_the_test_project_itself() =>
        Assert.DoesNotContain("Voluntro.ArchitectureTests", SolutionAssemblies.Graph.Keys);

    [Theory]
    [InlineData("System.Runtime")]
    [InlineData("System.Collections")]
    [InlineData("netstandard")]
    public void Runtime_assemblies_count_as_framework(string assemblyName) =>
        Assert.True(SolutionAssemblies.IsFrameworkAssembly(assemblyName));

    [Theory]
    [InlineData("xunit.v3.core")]
    [InlineData("Voluntro.SharedKernel")]
    [InlineData("Microsoft.AspNetCore.Http")]
    public void Packages_and_our_own_assemblies_do_not_count_as_framework(string assemblyName) =>
        Assert.False(SolutionAssemblies.IsFrameworkAssembly(assemblyName));

    /// <summary>The project names under src/BuildingBlocks and src/Modules (a project's file name is its assembly name).</summary>
    private static List<string> GuardedProjectNames()
    {
        var src = Path.Combine(RepositoryRoot(), "src");

        return _guardedFolders
            .Select(folder => Path.Combine(src, folder))
            .Where(Directory.Exists) // src/Modules does not exist until the first module does
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.csproj", SearchOption.AllDirectories))
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .ToList();
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Voluntro.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException($"Voluntro.slnx not found above {AppContext.BaseDirectory}.");
    }
}
