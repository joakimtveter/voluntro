using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// The registrar registers each command handler scoped, wrapped in the transaction decorator, and nothing else.
/// Resolving the decorator creates the module's context, which doesn't connect, so no database is needed.
/// </summary>
public sealed class CommandHandlerRegistrarTests
{
    [Fact]
    public void A_handler_with_a_value_resolves_wrapped_in_the_transaction_decorator()
    {
        using var provider = Build(commands => commands.Add<Ping, int, PingHandler>());
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<Ping, int>>();

        Assert.IsType<TransactionDecorator<Ping, int>>(handler);
    }

    [Fact]
    public void A_handler_without_a_value_resolves_wrapped_in_the_transaction_decorator()
    {
        using var provider = Build(commands => commands.Add<Poke, PokeHandler>());
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<Poke>>();

        Assert.IsType<TransactionDecorator<Poke>>(handler);
    }

    [Fact]
    public void A_validated_handler_with_a_value_resolves_wrapped_in_the_validation_decorator()
    {
        using var provider = Build(commands => commands.AddValidated<Ping, int, PingHandler, PingValidator>());
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<Ping, int>>();

        Assert.IsType<ValidationDecorator<Ping, int>>(handler);
    }

    [Fact]
    public void A_validated_handler_without_a_value_resolves_wrapped_in_the_validation_decorator()
    {
        using var provider = Build(commands => commands.AddValidated<Poke, PokeHandler, PokeValidator>());
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<Poke>>();

        Assert.IsType<ValidationDecorator<Poke>>(handler);
    }

    [Fact]
    public void The_validator_itself_is_not_registered()
    {
        using var provider = Build(commands => commands.AddValidated<Ping, int, PingHandler, PingValidator>());
        using var scope = provider.CreateScope();

        Assert.Null(scope.ServiceProvider.GetService<IValidator<Ping>>());
        Assert.Null(scope.ServiceProvider.GetService<PingValidator>());
    }

    [Fact]
    public void Registering_a_validated_handler_for_a_command_that_already_has_one_throws()
    {
        var commands = new ServiceCollection().AddCommandHandlers<PingDbContext>().Add<Ping, int, PingHandler>();

        Assert.Throws<InvalidOperationException>(() => commands.AddValidated<Ping, int, PingHandler, PingValidator>());
    }

    [Fact]
    public void Handlers_are_scoped()
    {
        using var provider = Build(commands => commands.Add<Ping, int, PingHandler>());
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var once = first.ServiceProvider.GetRequiredService<ICommandHandler<Ping, int>>();
        var again = first.ServiceProvider.GetRequiredService<ICommandHandler<Ping, int>>();
        var other = second.ServiceProvider.GetRequiredService<ICommandHandler<Ping, int>>();

        Assert.Same(once, again);
        Assert.NotSame(once, other);
    }

    [Fact]
    public void The_handler_class_itself_is_not_registered()
    {
        using var provider = Build(commands => commands.Add<Ping, int, PingHandler>());
        using var scope = provider.CreateScope();

        Assert.Null(scope.ServiceProvider.GetService<PingHandler>());
    }

    [Fact]
    public void Registering_a_second_handler_for_the_same_command_throws()
    {
        var commands = new ServiceCollection().AddCommandHandlers<PingDbContext>().Add<Ping, int, PingHandler>();

        Assert.Throws<InvalidOperationException>(() => commands.Add<Ping, int, PingHandler>());
    }

    private static ServiceProvider Build(Action<CommandHandlerRegistrar<PingDbContext>> register)
    {
        var services = new ServiceCollection();
        services.AddDbContext<PingDbContext>(options => options.UseNpgsql("Host=never-connected"));
        register(services.AddCommandHandlers<PingDbContext>());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private sealed record Ping;

    private sealed record Poke;

    private sealed class PingHandler : ICommandHandler<Ping, int>
    {
        public Task<Result<int>> HandleAsync(Ping command, CancellationToken cancellationToken) =>
            Task.FromResult<Result<int>>(1);
    }

    private sealed class PokeHandler : ICommandHandler<Poke>
    {
        public Task<Result> HandleAsync(Poke command, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed class PingValidator : IValidator<Ping>
    {
        public IReadOnlyList<FieldError> Validate(Ping instance) => [];
    }

    private sealed class PokeValidator : IValidator<Poke>
    {
        public IReadOnlyList<FieldError> Validate(Poke instance) => [];
    }

    private sealed class PingDbContext(DbContextOptions<PingDbContext> options) : ModuleDbContext(options)
    {
        protected override string Schema => "ping";

        protected override void ConfigureModel(ModelBuilder modelBuilder)
        {
        }
    }
}
