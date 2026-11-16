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

    // A missing property binds as null (MVC's implicit [Required] is off), so only a validator can catch it.
    [Fact]
    public void A_command_with_a_non_nullable_reference_property_needs_a_validator()
    {
        var commands = new ServiceCollection().AddCommandHandlers<PingDbContext>();

        var exception = Assert.Throws<InvalidOperationException>(() => commands.Add<Rename, RenameHandler>());

        Assert.Contains(nameof(Rename.Name), exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(Rename.Note), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_command_with_a_non_nullable_reference_property_can_be_registered_with_a_validator()
    {
        using var provider = Build(commands => commands.AddValidated<Rename, RenameHandler, RenameValidator>());
        using var scope = provider.CreateScope();

        Assert.IsType<ValidationDecorator<Rename>>(scope.ServiceProvider.GetRequiredService<ICommandHandler<Rename>>());
    }

    [Fact]
    public void A_handler_whose_dependency_is_not_registered_fails_when_the_container_is_validated()
    {
        var exception = Assert.Throws<AggregateException>(() => Build(commands => commands.Add<Poke, NeedyPokeHandler>()));

        Assert.Contains(nameof(INotRegistered), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_validator_whose_dependency_is_not_registered_fails_when_the_container_is_validated()
    {
        var exception = Assert.Throws<AggregateException>(() =>
            Build(commands => commands.AddValidated<Poke, PokeHandler, NeedyPokeValidator>()));

        Assert.Contains(nameof(INotRegistered), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_disposable_handler_is_disposed_with_its_scope()
    {
        var log = new DisposalLog();
        using var provider = Build(
            commands => commands.Add<Poke, DisposablePokeHandler>(),
            services => services.AddSingleton(log));

        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ICommandHandler<Poke>>();
            Assert.Equal(0, log.Disposed);
        }

        Assert.Equal(1, log.Disposed);
    }

    private static ServiceProvider Build(
        Action<CommandHandlerRegistrar<PingDbContext>> register,
        Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddDbContext<PingDbContext>(options => options.UseNpgsql("Host=never-connected"));
        configure?.Invoke(services);
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

    /// <summary>A required name, plus a note that may be left out.</summary>
    private sealed record Rename(string Name, string? Note, int Count);

    private sealed class RenameHandler : ICommandHandler<Rename>
    {
        public Task<Result> HandleAsync(Rename command, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed class RenameValidator : IValidator<Rename>
    {
        public IReadOnlyList<FieldError> Validate(Rename instance) => [];
    }

    private interface INotRegistered;

    private sealed class NeedyPokeHandler(INotRegistered dependency) : ICommandHandler<Poke>
    {
        public INotRegistered Dependency { get; } = dependency;

        public Task<Result> HandleAsync(Poke command, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed class NeedyPokeValidator(INotRegistered dependency) : IValidator<Poke>
    {
        public INotRegistered Dependency { get; } = dependency;

        public IReadOnlyList<FieldError> Validate(Poke instance) => [];
    }

    private sealed class DisposalLog
    {
        public int Disposed { get; set; }
    }

    private sealed class DisposablePokeHandler(DisposalLog log) : ICommandHandler<Poke>, IDisposable
    {
        public Task<Result> HandleAsync(Poke command, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());

        public void Dispose() => log.Disposed++;
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
