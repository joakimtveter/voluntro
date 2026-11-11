namespace Voluntro.ArchitectureTests;

public sealed class AssemblyClassifierTests
{
    [Fact]
    public void SharedKernel_is_classified_as_SharedKernel()
    {
        var kind = AssemblyClassifier.Classify("Voluntro.SharedKernel");

        Assert.Equal(new SharedKernelAssembly(), kind);
    }

    [Theory]
    [InlineData("Voluntro.BuildingBlocks.Contracts", Layer.Contracts)]
    [InlineData("Voluntro.BuildingBlocks.Application", Layer.Application)]
    [InlineData("Voluntro.BuildingBlocks.Infrastructure", Layer.Infrastructure)]
    public void BuildingBlocks_are_classified_by_layer(string name, Layer layer)
    {
        var kind = AssemblyClassifier.Classify(name);

        Assert.Equal(new BuildingBlockAssembly(layer), kind);
    }

    [Theory]
    [InlineData("Voluntro.Membership.Domain", "Membership", Layer.Domain)]
    [InlineData("Voluntro.Membership.Application", "Membership", Layer.Application)]
    [InlineData("Voluntro.Membership.Infrastructure", "Membership", Layer.Infrastructure)]
    [InlineData("Voluntro.Membership.Contracts", "Membership", Layer.Contracts)]
    [InlineData("Voluntro.SmallGroups.Domain", "SmallGroups", Layer.Domain)]
    [InlineData("Voluntro.CheckIn.Contracts", "CheckIn", Layer.Contracts)]
    public void Module_assemblies_are_classified_by_module_and_layer(string name, string module, Layer layer)
    {
        var kind = AssemblyClassifier.Classify(name);

        Assert.Equal(new ModuleAssembly(module, layer), kind);
    }

    [Fact]
    public void BuildingBlocks_is_never_treated_as_a_module()
    {
        // Matches the Voluntro.{Module}.{Layer} shape, but BuildingBlocks has no Domain layer
        // and is not a module, so it must not slip through as ModuleAssembly("BuildingBlocks", Domain).
        var kind = AssemblyClassifier.Classify("Voluntro.BuildingBlocks.Domain");

        Assert.Equal(new UnclassifiedAssembly(), kind);
    }

    [Theory]
    [InlineData("Voluntro.Membership.Domian")]          // typo in the layer
    [InlineData("Voluntro.Membership.Shared")]          // not one of the four layers
    [InlineData("Voluntro.Membership")]                 // no layer
    [InlineData("Voluntro.Small.Groups.Domain")]        // module name with more than one segment
    [InlineData("Voluntro.Membership.Domain.Tests")]    // extra trailing segment
    [InlineData("Voluntro..Domain")]                    // empty module name
    [InlineData("Voluntro.membership.Domain")]          // module name not PascalCase
    [InlineData("Voluntro.Membership.domain")]          // layer is case-sensitive
    [InlineData("voluntro.Membership.Domain")]          // prefix is case-sensitive
    [InlineData("Voluntro.SharedKernel.Tests")]         // only the exact SharedKernel name counts
    [InlineData("Voluntro.BuildingBlocks")]             // BuildingBlocks without a layer
    public void Anything_else_is_unclassified(string name)
    {
        var kind = AssemblyClassifier.Classify(name);

        Assert.Equal(new UnclassifiedAssembly(), kind);
    }
}
