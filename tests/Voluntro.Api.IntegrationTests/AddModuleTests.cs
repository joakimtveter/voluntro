using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.Api.IntegrationTests.Sample.Persistence;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Infrastructure;
using Voluntro.SharedKernel;

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

    // Handlers never save, so a handler outside the command pipeline would answer success and persist nothing.
    [Fact]
    public void A_command_handler_registered_without_the_registrar_is_rejected()
    {
        var module = new RegisteringModule(services => services.AddScoped<ICommandHandler<Bare>, BareHandler>());

        var error = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddModule(module, EmptyConfig));
        Assert.Contains(nameof(Bare), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_command_handler_with_a_value_registered_without_the_registrar_is_rejected()
    {
        var module = new RegisteringModule(services => services.AddScoped<ICommandHandler<Bare, int>, BareHandler>());

        var error = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddModule(module, EmptyConfig));
        Assert.Contains(nameof(Bare), error.Message, StringComparison.Ordinal);
    }

    // The last registration wins, so this one would replace the pipeline the registrar built.
    [Fact]
    public void A_command_handler_registered_directly_after_the_registrar_is_rejected()
    {
        var module = new RegisteringModule(services =>
        {
            services.AddCommandHandlers<SampleDbContext>().Add<Bare, BareHandler>();
            services.AddScoped<ICommandHandler<Bare>, BareHandler>();
        });

        var error = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddModule(module, EmptyConfig));
        Assert.Contains(nameof(Bare), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_handlers_registered_through_the_registrar_are_accepted()
    {
        var module = new RegisteringModule(services =>
            services.AddCommandHandlers<SampleDbContext>().Add<Bare, BareHandler>());

        new ServiceCollection().AddModule(module, EmptyConfig);
    }

    private sealed record Bare;

    private sealed class BareHandler : ICommandHandler<Bare>, ICommandHandler<Bare, int>
    {
        Task<Result> ICommandHandler<Bare>.HandleAsync(Bare command, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());

        Task<Result<int>> ICommandHandler<Bare, int>.HandleAsync(Bare command, CancellationToken cancellationToken) =>
            Task.FromResult<Result<int>>(1);
    }

    private sealed class RegisteringModule(Action<IServiceCollection> register) : IModule
    {
        public string Key => "registering";

        public bool IsCore => false;

        public IReadOnlyList<string> DependsOn => [];

        public void Register(IServiceCollection services, IConfiguration config) => register(services);
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
