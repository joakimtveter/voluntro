using ReferenceGraph = System.Collections.Generic.IReadOnlyDictionary<
    string,
    System.Collections.Generic.IReadOnlyCollection<string>>;

namespace Voluntro.ArchitectureTests;

/// <summary>
/// The layering rules as pure functions. Each takes a reference graph
/// (assembly name → names of the assemblies it references) and returns
/// one "A → B" string per forbidden reference, sorted. An empty list means the rule holds.
/// Only Voluntro.* references are judged; framework and package references are checked elsewhere.
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

    /// <summary>Voluntro assemblies in the graph whose names fit no convention; reported by name.</summary>
    public static IReadOnlyList<string> UnclassifiedAssemblies(ReferenceGraph graph) =>
        graph.Keys
            .Where(IsVoluntro)
            .Where(name => AssemblyClassifier.Classify(name) is UnclassifiedAssembly)
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Walks every reference between two Voluntro assemblies and reports the ones
    /// <paramref name="isForbidden"/> rejects. Each rule only has to describe a forbidden edge.
    /// </summary>
    private static List<string> ForbiddenReferences(
        ReferenceGraph graph,
        Func<AssemblyKind, AssemblyKind, bool> isForbidden)
    {
        var violations = new List<string>();

        foreach (var (assembly, references) in graph)
        {
            var from = AssemblyClassifier.Classify(assembly);

            foreach (var reference in references)
            {
                if (!IsVoluntro(reference))
                {
                    continue;
                }

                if (isForbidden(from, AssemblyClassifier.Classify(reference)))
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
