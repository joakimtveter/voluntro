using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.BuildingBlocks.Infrastructure;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// Registration mistakes fail at startup, before any request arrives.
/// The duplicate-key check runs first, so a clash on both key and assembly is reported as a key clash.
/// </summary>
public sealed class AddModuleTests
{
    private static readonly IConfiguration EmptyConfig = new ConfigurationBuilder().Build();

    [Fact]
    public void Two_modules_with_the_same_key_are_rejected()
    {
        var services = new ServiceCollection().AddModule(new SampleModule(), EmptyConfig);

        var error = Assert.Throws<InvalidOperationException>(() => services.AddModule(new SameKeyModule(), EmptyConfig));
        Assert.Contains("'sample'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Two_modules_in_the_same_assembly_are_rejected()
    {
        var services = new ServiceCollection().AddModule(new SampleModule(), EmptyConfig);

        // Controllers are mapped to their module by assembly, so one assembly can only hold one module.
        var error = Assert.Throws<InvalidOperationException>(() => services.AddModule(new SecondModule(), EmptyConfig));
        Assert.Contains(typeof(SampleModule).Assembly.GetName().Name!, error.Message, StringComparison.Ordinal);
    }

    private sealed class SameKeyModule : IModule
    {
        public string Key => "sample";

        public bool IsCore => false;

        public IReadOnlyList<string> DependsOn => [];

        public void Register(IServiceCollection services, IConfiguration config)
        {
        }
    }

    private sealed class SecondModule : IModule
    {
        public string Key => "second";

        public bool IsCore => false;

        public IReadOnlyList<string> DependsOn => [];

        public void Register(IServiceCollection services, IConfiguration config)
        {
        }
    }
}
