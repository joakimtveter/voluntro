using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Contracts;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// The registrar registers each command handler scoped, wrapped in the transaction decorator, and nothing else.
/// Resolving the decorator creates the module's context, which doesn't connect, so no database is needed.
/// </summary>
public sealed class ModuleHandlerRegistrarTests
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
        var commands = new ServiceCollection().AddHandlers<PingDbContext>().Add<Ping, int, PingHandler>();

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
        var commands = new ServiceCollection().AddHandlers<PingDbContext>().Add<Ping, int, PingHandler>();

        Assert.Throws<InvalidOperationException>(() => commands.Add<Ping, int, PingHandler>());
    }

    // A missing property binds as null (MVC's implicit [Required] is off), so only a validator can catch it.
    [Fact]
    public void A_command_with_a_non_nullable_reference_property_needs_a_validator()
    {
        var commands = new ServiceCollection().AddHandlers<PingDbContext>();

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

    // A missing version binds as 0, which only a validator's RequireVersion turns into a 400.
    [Fact]
    public void A_command_with_a_version_needs_a_validator()
    {
        var commands = new ServiceCollection().AddHandlers<PingDbContext>();

        var exception = Assert.Throws<InvalidOperationException>(() => commands.Add<Archive, ArchiveHandler>());

        Assert.Contains(nameof(VersionValidation.RequireVersion), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_command_with_a_version_can_be_registered_with_a_validator()
    {
        using var provider = Build(commands => commands.AddValidated<Archive, ArchiveHandler, ArchiveValidator>());
        using var scope = provider.CreateScope();

        Assert.IsType<ValidationDecorator<Archive>>(scope.ServiceProvider.GetRequiredService<ICommandHandler<Archive>>());
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

    // Each module saves through its own context: the unit of work is keyed by the context type and lives in the scope.
    [Fact]
    public void The_unit_of_work_is_scoped_and_keyed_by_the_context()
    {
        using var provider = Build(_ => { });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var once = first.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(typeof(PingDbContext));
        var again = first.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(typeof(PingDbContext));
        var other = second.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(typeof(PingDbContext));

        Assert.Same(once, again);
        Assert.NotSame(once, other);
        Assert.Null(first.ServiceProvider.GetService<IUnitOfWork>());
    }

    // Only the module's unit of work calls a domain event handler, so nothing else can resolve one.
    [Fact]
    public void A_domain_event_handler_is_not_registered_as_such()
    {
        using var provider = Build(handlers => handlers.AddDomainEventHandler<Pinged, PingedHandler>());
        using var scope = provider.CreateScope();

        Assert.Null(scope.ServiceProvider.GetService<IDomainEventHandler<Pinged>>());
        Assert.Null(scope.ServiceProvider.GetService<PingedHandler>());
    }

    // It would run twice for every event.
    [Fact]
    public void Registering_the_same_domain_event_handler_twice_throws()
    {
        var handlers = new ServiceCollection().AddHandlers<PingDbContext>().AddDomainEventHandler<Pinged, PingedHandler>();

        Assert.Throws<InvalidOperationException>(() => handlers.AddDomainEventHandler<Pinged, PingedHandler>());
    }

    // Two AddHandlers calls for the same context add to the same list, so the duplicate is still found.
    [Fact]
    public void Registering_the_same_domain_event_handler_in_a_second_registrar_throws()
    {
        var services = new ServiceCollection();
        services.AddHandlers<PingDbContext>().AddDomainEventHandler<Pinged, PingedHandler>();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddHandlers<PingDbContext>().AddDomainEventHandler<Pinged, PingedHandler>());
    }

    // The unit of work matches the raised event's exact type: Moved raised as MovedAway would never reach the handler.
    [Fact]
    public void A_domain_event_handler_for_an_event_type_that_is_not_sealed_throws()
    {
        var handlers = new ServiceCollection().AddHandlers<PingDbContext>();

        var exception = Assert.Throws<InvalidOperationException>(() => handlers.AddDomainEventHandler<Moved, MovedHandler>());

        Assert.Contains(nameof(Moved), exception.Message, StringComparison.Ordinal);
    }

    // A test's own unit of work, registered first, wins over the registrar's. It's fine for command handlers, but it
    // would never dispatch a domain event or save a consumer's inbox record.
    [Fact]
    public void An_event_handler_throws_when_another_unit_of_work_was_registered_for_the_context()
    {
        var services = new ServiceCollection();
        services.AddKeyedScoped<IUnitOfWork, StandInUnitOfWork>(typeof(PingDbContext));
        var handlers = services.AddHandlers<PingDbContext>().Add<Poke, PokeHandler>();

        Assert.Throws<InvalidOperationException>(() => handlers.AddDomainEventHandler<Pinged, PingedHandler>());
        Assert.Throws<InvalidOperationException>(() => handlers.AddIntegrationEventHandler<Shipped, ShippedHandler>());
    }

    // The second registrar finds the first one's unit of work, not a stranger's.
    [Fact]
    public void An_event_handler_can_be_registered_through_a_second_registrar_for_the_context()
    {
        var services = new ServiceCollection();
        services.AddHandlers<PingDbContext>().AddDomainEventHandler<Pinged, PingedHandler>();

        services.AddHandlers<PingDbContext>().AddIntegrationEventHandler<Shipped, ShippedHandler>();
    }

    [Fact]
    public void A_domain_event_handler_whose_dependency_is_not_registered_fails_when_the_container_is_validated()
    {
        var exception = Assert.Throws<AggregateException>(() =>
            Build(handlers => handlers.AddDomainEventHandler<Pinged, NeedyPingedHandler>()));

        Assert.Contains(nameof(INotRegistered), exception.Message, StringComparison.Ordinal);
    }

    // Only the outbox processors call a consumer, through its inbox; nothing else can resolve one.
    [Fact]
    public void An_integration_event_handler_is_not_registered_as_such()
    {
        using var provider = Build(handlers => handlers.AddIntegrationEventHandler<Shipped, ShippedHandler>());
        using var scope = provider.CreateScope();

        Assert.Null(scope.ServiceProvider.GetService<IIntegrationEventHandler<Shipped>>());
        Assert.Null(scope.ServiceProvider.GetService<ShippedHandler>());
    }

    // Both would share one inbox record, so the second would never run.
    [Fact]
    public void Registering_the_same_integration_event_handler_twice_throws()
    {
        var services = new ServiceCollection();
        services.AddHandlers<PingDbContext>().AddIntegrationEventHandler<Shipped, ShippedHandler>();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddHandlers<PingDbContext>().AddIntegrationEventHandler<Shipped, ShippedHandler>());
    }

    [Fact]
    public void Two_consumers_of_one_integration_event_can_be_registered()
    {
        using var provider = Build(handlers => handlers
            .AddIntegrationEventHandler<Shipped, ShippedHandler>()
            .AddIntegrationEventHandler<Shipped, AlsoShippedHandler>());

        Assert.NotNull(provider);
    }

    [Fact]
    public void An_integration_event_handler_whose_dependency_is_not_registered_fails_when_the_container_is_validated()
    {
        var exception = Assert.Throws<AggregateException>(() =>
            Build(handlers => handlers.AddIntegrationEventHandler<Shipped, NeedyShippedHandler>()));

        Assert.Contains(nameof(INotRegistered), exception.Message, StringComparison.Ordinal);
    }

    private static ServiceProvider Build(
        Action<ModuleHandlerRegistrar<PingDbContext>> register,
        Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddDbContext<PingDbContext>(options => options.UseNpgsql("Host=never-connected"));
        configure?.Invoke(services);
        register(services.AddHandlers<PingDbContext>());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private sealed record Ping;

    private sealed record Poke;

    private sealed record Pinged : IDomainEvent;

    private record Moved : IDomainEvent;

    private sealed record MovedAway : Moved;

    private sealed record Shipped : IIntegrationEvent
    {
        public static string EventType => "ping.shipped.v1";
    }

    private sealed class ShippedHandler : IIntegrationEventHandler<Shipped>
    {
        public Task<Result> HandleAsync(Shipped integrationEvent, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed class AlsoShippedHandler : IIntegrationEventHandler<Shipped>
    {
        public Task<Result> HandleAsync(Shipped integrationEvent, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed class NeedyShippedHandler(INotRegistered dependency) : IIntegrationEventHandler<Shipped>
    {
        public INotRegistered Dependency { get; } = dependency;

        public Task<Result> HandleAsync(Shipped integrationEvent, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed class PingedHandler : IDomainEventHandler<Pinged>
    {
        public Task<Result> HandleAsync(Pinged domainEvent, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed class StandInUnitOfWork : IUnitOfWork
    {
        public Task<Result> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success());

        public void DiscardChanges()
        {
        }
    }

    private sealed class MovedHandler : IDomainEventHandler<Moved>
    {
        public Task<Result> HandleAsync(Moved domainEvent, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed class NeedyPingedHandler(INotRegistered dependency) : IDomainEventHandler<Pinged>
    {
        public INotRegistered Dependency { get; } = dependency;

        public Task<Result> HandleAsync(Pinged domainEvent, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

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

    /// <summary>An update made of value types only: nothing binds as null, but the version can be left out.</summary>
    private sealed record Archive(Guid Id, uint Version);

    private sealed class ArchiveHandler : ICommandHandler<Archive>
    {
        public Task<Result> HandleAsync(Archive command, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    private sealed class ArchiveValidator : IValidator<Archive>
    {
        public IReadOnlyList<FieldError> Validate(Archive instance)
        {
            var errors = new List<FieldError>();
            errors.RequireVersion(instance.Version);
            return errors;
        }
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
