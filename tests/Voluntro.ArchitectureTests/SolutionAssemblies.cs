using System.Reflection;
using System.Runtime.InteropServices;

namespace Voluntro.ArchitectureTests;

/// <summary>
/// The real reference graph of the solution: every Voluntro.*.dll that the wildcard
/// ProjectReferences copied into this test project's output folder, mapped to the names
/// of the assemblies it references. Built once per test run.
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
        var testAssembly = typeof(SolutionAssemblies).Assembly.GetName().Name;
        var graph = new Dictionary<string, IReadOnlyCollection<string>>();

        foreach (var path in Directory.EnumerateFiles(AppContext.BaseDirectory, "Voluntro.*.dll"))
        {
            var name = AssemblyName.GetAssemblyName(path);

            if (name.Name == testAssembly)
            {
                continue;
            }

            var assembly = Assembly.Load(name);

            graph[name.Name!] = assembly
                .GetReferencedAssemblies()
                .Select(reference => reference.Name!)
                .ToList();
        }

        return graph;
    }
}
