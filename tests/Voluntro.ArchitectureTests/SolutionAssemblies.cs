using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Voluntro.ArchitectureTests;

/// <summary>
/// The real reference graph of the solution: every Voluntro.* assembly that the wildcard
/// ProjectReferences brought into this test project, mapped to the names of the assemblies
/// it references. Built once per test run.
/// </summary>
public static class SolutionAssemblies
{
    private static readonly Lazy<ReferenceGraph> _graph = new(Load);

    public static ReferenceGraph Graph => _graph.Value;

    /// <summary>True when the assembly ships with the .NET runtime (Microsoft.NETCore.App) rather than a package.</summary>
    public static bool IsFrameworkAssembly(string assemblyName) =>
        File.Exists(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), assemblyName + ".dll"));

    private static Dictionary<string, IReadOnlyCollection<string>> Load()
    {
        var testAssembly = typeof(SolutionAssemblies).Assembly.GetName().Name!;
        var graph = new Dictionary<string, IReadOnlyCollection<string>>();

        foreach (var fileName in CurrentVoluntroAssemblyFiles(testAssembly))
        {
            var (name, references) = ReadReferences(Path.Combine(AppContext.BaseDirectory, fileName));
            graph[name] = references;
        }

        return graph;
    }

    /// <summary>
    /// The Voluntro.*.dll files this build depends on, taken from the test project's deps.json
    /// rather than from the output folder. The folder can hold DLLs left over from an earlier
    /// build (a renamed or deleted project, another branch); deps.json is rewritten on every
    /// build, so leftovers are not in it.
    /// </summary>
    private static List<string> CurrentVoluntroAssemblyFiles(string testAssembly)
    {
        var depsFile = Path.Combine(AppContext.BaseDirectory, testAssembly + ".deps.json");
        using var deps = JsonDocument.Parse(File.ReadAllText(depsFile));

        return deps.RootElement
            .GetProperty("targets")
            .EnumerateObject()
            .SelectMany(target => target.Value.EnumerateObject())
            .Where(library => library.Value.TryGetProperty("runtime", out _))
            .SelectMany(library => library.Value.GetProperty("runtime").EnumerateObject())
            .Select(runtimeFile => Path.GetFileName(runtimeFile.Name))
            .Where(fileName =>
                fileName.StartsWith("Voluntro.", StringComparison.Ordinal)
                && fileName.EndsWith(".dll", StringComparison.Ordinal)
                && fileName != testAssembly + ".dll")
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Reads the assembly's name and its references straight from the file's metadata, without
    /// loading it, so the file inspected is exactly the file at <paramref name="path"/>.
    /// </summary>
    private static (string Name, IReadOnlyCollection<string> References) ReadReferences(string path)
    {
        using var stream = File.OpenRead(path);
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();

        var name = metadata.GetString(metadata.GetAssemblyDefinition().Name);
        var references = metadata.AssemblyReferences
            .Select(handle => metadata.GetString(metadata.GetAssemblyReference(handle).Name))
            .ToList();

        return (name, references);
    }
}
