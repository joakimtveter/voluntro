using System.Reflection;
using System.Runtime.InteropServices;
using Voluntro.SharedKernel;

namespace Voluntro.ArchitectureTests;

public sealed class SharedKernelTests
{
    [Fact]
    public void SharedKernel_depends_only_on_the_dotnet_runtime()
    {
        // Every assembly SharedKernel references must ship with the shared framework
        // (Microsoft.NETCore.App). Anything else came from a package or another project.
        var sharedKernel = typeof(Result).Assembly;

        var foreignReferences = sharedKernel
            .GetReferencedAssemblies()
            .Where(reference => !IsSharedFrameworkAssembly(reference))
            .Select(reference => reference.Name)
            .ToList();

        Assert.Empty(foreignReferences);
    }

    private static bool IsSharedFrameworkAssembly(AssemblyName reference) =>
        File.Exists(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), reference.Name + ".dll"));
}
