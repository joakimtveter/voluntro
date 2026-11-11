namespace Voluntro.ArchitectureTests;

/// <summary>
/// The layering rules as pure functions over made-up reference graphs (assembly name → names it references).
/// Every rule must fire here, so it is proven to work before any real module exists.
/// The rules only judge Voluntro.* references; framework and package references are checked separately.
/// </summary>
public sealed class LayeringRulesTests
{
    private const string SharedKernel = "Voluntro.SharedKernel";
    private const string BbContracts = "Voluntro.BuildingBlocks.Contracts";
    private const string BbApplication = "Voluntro.BuildingBlocks.Application";
    private const string BbInfrastructure = "Voluntro.BuildingBlocks.Infrastructure";

    private const string MembershipDomain = "Voluntro.Membership.Domain";
    private const string MembershipApplication = "Voluntro.Membership.Application";
    private const string MembershipInfrastructure = "Voluntro.Membership.Infrastructure";
    private const string MembershipContracts = "Voluntro.Membership.Contracts";
    private const string MembershipShared = "Voluntro.Membership.Shared"; // unclassified

    private const string GroupsDomain = "Voluntro.SmallGroups.Domain";
    private const string GroupsApplication = "Voluntro.SmallGroups.Application";
    private const string GroupsInfrastructure = "Voluntro.SmallGroups.Infrastructure";
    private const string GroupsContracts = "Voluntro.SmallGroups.Contracts";

    private const string Framework = "System.Runtime";
    private const string Package = "Microsoft.EntityFrameworkCore";

    // ── SharedKernel ────────────────────────────────────────────────────────────

    [Fact]
    public void SharedKernel_may_reference_the_framework()
    {
        var graph = Graph((SharedKernel, [Framework]));

        Assert.Empty(LayeringRules.SharedKernelReferencesNoVoluntroAssembly(graph));
    }

    [Fact]
    public void SharedKernel_referencing_a_Voluntro_assembly_is_a_violation()
    {
        var graph = Graph((SharedKernel, [Framework, BbApplication]));

        var violation = Assert.Single(LayeringRules.SharedKernelReferencesNoVoluntroAssembly(graph));
        Assert.Equal($"{SharedKernel} → {BbApplication}", violation);
    }

    // ── Module Domain ───────────────────────────────────────────────────────────

    [Fact]
    public void Domain_may_reference_SharedKernel_and_non_Voluntro_assemblies()
    {
        var graph = Graph((MembershipDomain, [Framework, Package, SharedKernel]));

        Assert.Empty(LayeringRules.DomainReferencesOnlySharedKernel(graph));
    }

    [Theory]
    [InlineData(BbContracts)]
    [InlineData(BbApplication)]
    [InlineData(MembershipApplication)]
    [InlineData(MembershipContracts)]
    [InlineData(GroupsContracts)]
    [InlineData(MembershipShared)]
    public void Domain_referencing_any_other_Voluntro_assembly_is_a_violation(string forbidden)
    {
        var graph = Graph((MembershipDomain, [SharedKernel, forbidden]));

        var violation = Assert.Single(LayeringRules.DomainReferencesOnlySharedKernel(graph));
        Assert.Equal($"{MembershipDomain} → {forbidden}", violation);
    }

    [Fact]
    public void Domain_rule_applies_to_every_module()
    {
        var graph = Graph(
            (MembershipDomain, [SharedKernel]),
            (GroupsDomain, [SharedKernel, BbApplication]));

        var violation = Assert.Single(LayeringRules.DomainReferencesOnlySharedKernel(graph));
        Assert.Equal($"{GroupsDomain} → {BbApplication}", violation);
    }

    // ── Module Contracts ────────────────────────────────────────────────────────

    [Fact]
    public void Contracts_may_reference_SharedKernel_and_BuildingBlocks_Contracts()
    {
        var graph = Graph((MembershipContracts, [Framework, SharedKernel, BbContracts]));

        Assert.Empty(LayeringRules.ContractsReferenceOnlySharedKernelAndBuildingBlockContracts(graph));
    }

    [Theory]
    [InlineData(BbApplication)]
    [InlineData(MembershipDomain)]
    [InlineData(GroupsContracts)] // Contracts are leaves
    public void Contracts_referencing_anything_else_is_a_violation(string forbidden)
    {
        var graph = Graph((MembershipContracts, [SharedKernel, forbidden]));

        var violation = Assert.Single(LayeringRules.ContractsReferenceOnlySharedKernelAndBuildingBlockContracts(graph));
        Assert.Equal($"{MembershipContracts} → {forbidden}", violation);
    }

    // ── Module Application ──────────────────────────────────────────────────────

    [Fact]
    public void Application_may_reference_everything_below_Infrastructure()
    {
        var graph = Graph((MembershipApplication,
            [Framework, Package, SharedKernel, BbContracts, BbApplication, MembershipDomain, MembershipContracts, GroupsContracts]));

        Assert.Empty(LayeringRules.ApplicationReferencesNoInfrastructure(graph));
    }

    [Theory]
    [InlineData(MembershipInfrastructure)]
    [InlineData(BbInfrastructure)]
    [InlineData(GroupsInfrastructure)]
    public void Application_referencing_any_Infrastructure_is_a_violation(string forbidden)
    {
        var graph = Graph((MembershipApplication, [SharedKernel, forbidden]));

        var violation = Assert.Single(LayeringRules.ApplicationReferencesNoInfrastructure(graph));
        Assert.Equal($"{MembershipApplication} → {forbidden}", violation);
    }

    // ── Across modules ──────────────────────────────────────────────────────────

    [Fact]
    public void Modules_may_reference_their_own_layers_and_other_modules_Contracts()
    {
        var graph = Graph(
            (MembershipApplication, [MembershipDomain, MembershipContracts, GroupsContracts]),
            (MembershipInfrastructure, [MembershipApplication, MembershipDomain, GroupsContracts]));

        Assert.Empty(LayeringRules.ModulesReferenceEachOtherOnlyThroughContracts(graph));
    }

    [Theory]
    [InlineData(GroupsDomain)]
    [InlineData(GroupsApplication)]
    [InlineData(GroupsInfrastructure)]
    public void Referencing_another_module_outside_its_Contracts_is_a_violation(string forbidden)
    {
        var graph = Graph((MembershipInfrastructure, [MembershipApplication, forbidden]));

        var violation = Assert.Single(LayeringRules.ModulesReferenceEachOtherOnlyThroughContracts(graph));
        Assert.Equal($"{MembershipInfrastructure} → {forbidden}", violation);
    }

    // ── BuildingBlocks ──────────────────────────────────────────────────────────

    [Fact]
    public void BuildingBlocks_Contracts_may_reference_SharedKernel()
    {
        var graph = Graph((BbContracts, [Framework, SharedKernel]));

        Assert.Empty(LayeringRules.BuildingBlockContractsReferenceOnlySharedKernel(graph));
    }

    [Theory]
    [InlineData(BbApplication)]
    [InlineData(BbInfrastructure)]
    public void BuildingBlocks_Contracts_referencing_another_BuildingBlock_is_a_violation(string forbidden)
    {
        var graph = Graph((BbContracts, [SharedKernel, forbidden]));

        var violation = Assert.Single(LayeringRules.BuildingBlockContractsReferenceOnlySharedKernel(graph));
        Assert.Equal($"{BbContracts} → {forbidden}", violation);
    }

    [Fact]
    public void BuildingBlocks_Application_may_reference_SharedKernel_and_BuildingBlocks_Contracts()
    {
        var graph = Graph((BbApplication, [Framework, SharedKernel, BbContracts]));

        Assert.Empty(LayeringRules.BuildingBlockApplicationReferencesNoInfrastructure(graph));
    }

    [Fact]
    public void BuildingBlocks_Application_referencing_BuildingBlocks_Infrastructure_is_a_violation()
    {
        var graph = Graph((BbApplication, [SharedKernel, BbInfrastructure]));

        var violation = Assert.Single(LayeringRules.BuildingBlockApplicationReferencesNoInfrastructure(graph));
        Assert.Equal($"{BbApplication} → {BbInfrastructure}", violation);
    }

    [Fact]
    public void BuildingBlocks_may_reference_each_other_and_SharedKernel()
    {
        var graph = Graph(
            (BbApplication, [SharedKernel, BbContracts]),
            (BbInfrastructure, [SharedKernel, BbContracts, BbApplication]));

        Assert.Empty(LayeringRules.BuildingBlocksReferenceNoModule(graph));
    }

    [Theory]
    [InlineData(BbContracts)]
    [InlineData(BbApplication)]
    [InlineData(BbInfrastructure)]
    public void BuildingBlocks_referencing_a_module_is_a_violation(string buildingBlock)
    {
        var graph = Graph((buildingBlock, [SharedKernel, MembershipContracts]));

        var violation = Assert.Single(LayeringRules.BuildingBlocksReferenceNoModule(graph));
        Assert.Equal($"{buildingBlock} → {MembershipContracts}", violation);
    }

    // ── Unclassified ────────────────────────────────────────────────────────────

    [Fact]
    public void Unclassified_Voluntro_assemblies_are_reported_by_name()
    {
        var graph = Graph(
            (SharedKernel, []),
            (MembershipDomain, [SharedKernel]),
            (MembershipShared, [SharedKernel]),
            (Package, []));

        var unclassified = Assert.Single(LayeringRules.UnclassifiedAssemblies(graph));
        Assert.Equal(MembershipShared, unclassified);
    }

    // ── Packages ────────────────────────────────────────────────────────────────

    private static bool IsFakeFramework(string name) => name.StartsWith("System.", StringComparison.Ordinal);

    [Theory]
    [InlineData(SharedKernel)]
    [InlineData(MembershipDomain)]
    [InlineData(MembershipContracts)]
    [InlineData(BbContracts)]
    public void Framework_only_assemblies_may_reference_the_framework(string assembly)
    {
        var graph = Graph((assembly, [Framework, "System.Collections"]));

        Assert.Empty(LayeringRules.FrameworkOnlyAssembliesReferenceNoPackages(graph, IsFakeFramework));
    }

    [Theory]
    [InlineData(SharedKernel)]
    [InlineData(MembershipDomain)]
    [InlineData(MembershipContracts)]
    [InlineData(BbContracts)]
    public void Framework_only_assemblies_referencing_a_package_is_a_violation(string assembly)
    {
        var graph = Graph((assembly, [Framework, Package]));

        var violation = Assert.Single(LayeringRules.FrameworkOnlyAssembliesReferenceNoPackages(graph, IsFakeFramework));
        Assert.Equal($"{assembly} → {Package}", violation);
    }

    [Theory]
    [InlineData(MembershipApplication)]
    [InlineData(MembershipInfrastructure)]
    [InlineData(BbApplication)]
    [InlineData(BbInfrastructure)]
    public void Other_assemblies_may_reference_packages(string assembly)
    {
        var graph = Graph((assembly, [Framework, Package]));

        Assert.Empty(LayeringRules.FrameworkOnlyAssembliesReferenceNoPackages(graph, IsFakeFramework));
    }

    [Fact]
    public void Package_rule_leaves_Voluntro_references_to_the_layering_rules()
    {
        var graph = Graph((MembershipDomain, [Framework, MembershipApplication]));

        Assert.Empty(LayeringRules.FrameworkOnlyAssembliesReferenceNoPackages(graph, IsFakeFramework));
    }

    // ── Reporting ───────────────────────────────────────────────────────────────

    [Fact]
    public void Violations_are_reported_in_ordinal_order_so_failures_are_stable()
    {
        // Inserted out of order on purpose: SmallGroups before Membership, Infrastructure before BuildingBlocks.
        var graph = Graph(
            (GroupsDomain, [GroupsApplication]),
            (MembershipDomain, [MembershipInfrastructure, BbApplication]));

        Assert.Collection(
            LayeringRules.DomainReferencesOnlySharedKernel(graph),
            v => Assert.Equal($"{MembershipDomain} → {BbApplication}", v),
            v => Assert.Equal($"{MembershipDomain} → {MembershipInfrastructure}", v),
            v => Assert.Equal($"{GroupsDomain} → {GroupsApplication}", v));
    }

    private static Dictionary<string, IReadOnlyCollection<string>> Graph(
        params (string Assembly, string[] References)[] nodes) =>
        nodes.ToDictionary(node => node.Assembly, IReadOnlyCollection<string> (node) => node.References);
}
