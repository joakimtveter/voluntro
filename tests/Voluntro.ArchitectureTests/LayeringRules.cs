global using ReferenceGraph = System.Collections.Generic.IReadOnlyDictionary<
    string,
    System.Collections.Generic.IReadOnlyCollection<string>>;

namespace Voluntro.ArchitectureTests;

/// <summary>
/// The layering rules as pure functions. Each takes a reference graph
/// (assembly name → names of the assemblies it references) and returns
/// one "A → B" string per forbidden reference, sorted. An empty list means the rule holds.
/// The layering rules judge only Voluntro.* references; the package rule judges only the rest.
/// </summary>
public static class LayeringRules
{
    private const string VoluntroPrefix = "Voluntro.";

    public static IReadOnlyList<string> SharedKernelReferencesNoVoluntroAssembly(ReferenceGraph graph) =>
        ForbiddenReferences(graph, (from, _) => from is SharedKernelAssembly);

    public static IReadOnlyList<string> DomainReferencesOnlySharedKernel(ReferenceGraph graph) =>
        ForbiddenReferences(graph, (from, to) =>
            (from, to) is (ModuleAssembly { Layer: Layer.Domain }, not SharedKernelAssembly));

    public static IReadOnlyList<string> ContractsReferenceOnlySharedKernelAndBuildingBlockContracts(ReferenceGraph graph) =>
        ForbiddenReferences(graph, (from, to) =>
            (from, to) is (
                ModuleAssembly { Layer: Layer.Contracts },
                not (SharedKernelAssembly or BuildingBlockAssembly { Layer: Layer.Contracts })));

    public static IReadOnlyList<string> ApplicationReferencesNoInfrastructure(ReferenceGraph graph) =>
        ForbiddenReferences(graph, (from, to) =>
            (from, to) is (
                ModuleAssembly { Layer: Layer.Application },
                ModuleAssembly { Layer: Layer.Infrastructure } or BuildingBlockAssembly { Layer: Layer.Infrastructure }));

    public static IReadOnlyList<string> ModulesReferenceEachOtherOnlyThroughContracts(ReferenceGraph graph) =>
        ForbiddenReferences(graph, (from, to) =>
            (from, to) is (ModuleAssembly source, ModuleAssembly { Layer: not Layer.Contracts } target)
            && source.Module != target.Module);

    public static IReadOnlyList<string> BuildingBlockContractsReferenceOnlySharedKernel(ReferenceGraph graph) =>
        ForbiddenReferences(graph, (from, to) =>
            (from, to) is (BuildingBlockAssembly { Layer: Layer.Contracts }, not SharedKernelAssembly));

    public static IReadOnlyList<string> BuildingBlockApplicationReferencesNoInfrastructure(ReferenceGraph graph) =>
        ForbiddenReferences(graph, (from, to) =>
            (from, to) is (
                BuildingBlockAssembly { Layer: Layer.Application },
                BuildingBlockAssembly { Layer: Layer.Infrastructure }));

    public static IReadOnlyList<string> BuildingBlocksReferenceNoModule(ReferenceGraph graph) =>
        ForbiddenReferences(graph, (from, to) => (from, to) is (BuildingBlockAssembly, ModuleAssembly));

    public static IReadOnlyList<string> FrameworkOnlyAssembliesReferenceNoPackages(
        ReferenceGraph graph,
        Func<string, bool> isFramework) =>
        Violations(graph, (from, to) =>
            IsFrameworkOnly(AssemblyClassifier.Classify(from)) && !IsVoluntro(to) && !isFramework(to));

    private static bool IsFrameworkOnly(AssemblyKind kind) =>
        kind is SharedKernelAssembly
            or ModuleAssembly { Layer: Layer.Domain or Layer.Contracts }
            or BuildingBlockAssembly { Layer: Layer.Contracts };

    /// <summary>Voluntro assemblies in the graph whose names fit no convention; reported by name.</summary>
    public static IReadOnlyList<string> UnclassifiedAssemblies(ReferenceGraph graph) =>
        graph.Keys
            .Where(IsVoluntro)
            .Where(name => AssemblyClassifier.Classify(name) is UnclassifiedAssembly)
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>Layering rules: judges only references between two Voluntro assemblies, by kind.</summary>
    private static List<string> ForbiddenReferences(
        ReferenceGraph graph,
        Func<AssemblyKind, AssemblyKind, bool> isForbidden) =>
        Violations(graph, (from, to) =>
            IsVoluntro(to)
            && isForbidden(AssemblyClassifier.Classify(from), AssemblyClassifier.Classify(to)));

    /// <summary>Walks every reference in the graph and reports the ones <paramref name="isForbidden"/> rejects.</summary>
    private static List<string> Violations(ReferenceGraph graph, Func<string, string, bool> isForbidden)
    {
        var violations = new List<string>();

        foreach (var (assembly, references) in graph)
        {
            foreach (var reference in references)
            {
                if (isForbidden(assembly, reference))
                {
                    violations.Add($"{assembly} → {reference}");
                }
            }
        }

        violations.Sort(StringComparer.Ordinal);
        return violations;
    }

    private static bool IsVoluntro(string assemblyName) =>
        assemblyName.StartsWith(VoluntroPrefix, StringComparison.Ordinal);
}
