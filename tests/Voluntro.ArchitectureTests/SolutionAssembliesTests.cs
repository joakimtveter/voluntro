namespace Voluntro.ArchitectureTests;

public sealed class SolutionAssembliesTests
{
    [Theory]
    [InlineData("Voluntro.SharedKernel")]
    [InlineData("Voluntro.BuildingBlocks.Application")]
    [InlineData("Voluntro.BuildingBlocks.Infrastructure")]
    public void Scan_finds_the_core_assemblies(string assemblyName) =>
        Assert.Contains(assemblyName, SolutionAssemblies.Graph.Keys);

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
}
