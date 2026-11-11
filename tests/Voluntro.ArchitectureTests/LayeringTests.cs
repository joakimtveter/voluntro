namespace Voluntro.ArchitectureTests;

/// <summary>The layering rules run against the real solution. See docs/architecture/overview.md.</summary>
public sealed class LayeringTests
{
    private static ReferenceGraph Solution => SolutionAssemblies.Graph;

    [Fact]
    public void SharedKernel_references_no_other_Voluntro_assembly() =>
        AssertNoForbiddenReferences(LayeringRules.SharedKernelReferencesNoVoluntroAssembly(Solution));

    [Fact]
    public void SharedKernel_BuildingBlocks_Contracts_Domain_and_Contracts_reference_no_packages() =>
        AssertNoForbiddenReferences(
            LayeringRules.FrameworkOnlyAssembliesReferenceNoPackages(Solution, SolutionAssemblies.IsFrameworkAssembly));

    [Fact]
    public void Domain_references_only_SharedKernel() =>
        AssertNoForbiddenReferences(LayeringRules.DomainReferencesOnlySharedKernel(Solution));

    [Fact]
    public void Contracts_reference_only_SharedKernel_and_BuildingBlocks_Contracts() =>
        AssertNoForbiddenReferences(LayeringRules.ContractsReferenceOnlySharedKernelAndBuildingBlockContracts(Solution));

    [Fact]
    public void Application_references_no_Infrastructure() =>
        AssertNoForbiddenReferences(LayeringRules.ApplicationReferencesNoInfrastructure(Solution));

    [Fact]
    public void Modules_reference_each_other_only_through_Contracts() =>
        AssertNoForbiddenReferences(LayeringRules.ModulesReferenceEachOtherOnlyThroughContracts(Solution));

    [Fact]
    public void BuildingBlocks_Contracts_references_only_SharedKernel() =>
        AssertNoForbiddenReferences(LayeringRules.BuildingBlockContractsReferenceOnlySharedKernel(Solution));

    [Fact]
    public void BuildingBlocks_Application_references_no_Infrastructure() =>
        AssertNoForbiddenReferences(LayeringRules.BuildingBlockApplicationReferencesNoInfrastructure(Solution));

    [Fact]
    public void BuildingBlocks_reference_no_module() =>
        AssertNoForbiddenReferences(LayeringRules.BuildingBlocksReferenceNoModule(Solution));

    [Fact]
    public void Every_Voluntro_assembly_follows_the_naming_convention()
    {
        var unclassified = LayeringRules.UnclassifiedAssemblies(Solution);

        if (unclassified.Count > 0)
        {
            Assert.Fail(
                "Not SharedKernel, Voluntro.BuildingBlocks.{Layer} or Voluntro.{Module}.{Layer}:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, unclassified));
        }
    }

    private static void AssertNoForbiddenReferences(IReadOnlyList<string> violations)
    {
        if (violations.Count > 0)
        {
            Assert.Fail(
                $"{violations.Count} forbidden reference(s):"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
        }
    }
}
