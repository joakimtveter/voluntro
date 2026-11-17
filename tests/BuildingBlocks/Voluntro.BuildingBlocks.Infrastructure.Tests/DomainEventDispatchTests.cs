using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// The module's unit of work runs the handlers of the domain events raised by tracked aggregates before it saves,
/// round after round while handlers raise more (ADR-0013). Handlers and the save record their calls in one log; the
/// context's save is overridden to record instead of reaching a database.
/// </summary>
public sealed class DomainEventDispatchTests
{
    // The limit ADR-0013 sets; pinned here, so changing it in the unit of work fails a test.
    private const int MaxRounds = 10;

    private static readonly Error Refused = Error.Conflict("test.event.refused");

    [Fact]
    public async Task A_handler_receives_the_event_before_the_save()
    {
        var (log, result) = await SaveAsync(
            handlers => handlers.AddDomainEventHandler<Pinged, FirstPingedHandler>(),
            counter => counter.Emit(new Pinged("a")));

        Assert.True(result.IsSuccess);
        Assert.Equal(["first:a", "save"], log);
    }

    [Fact]
    public async Task Several_handlers_for_one_event_run_in_registration_order()
    {
        var (log, _) = await SaveAsync(
            handlers => handlers
                .AddDomainEventHandler<Pinged, SecondPingedHandler>()
                .AddDomainEventHandler<Pinged, FirstPingedHandler>(),
            counter => counter.Emit(new Pinged("a")));

        Assert.Equal(["second:a", "first:a", "save"], log);
    }

    [Fact]
    public async Task Events_are_handled_in_the_order_they_were_raised()
    {
        var (log, _) = await SaveAsync(
            handlers => handlers.AddDomainEventHandler<Pinged, FirstPingedHandler>(),
            counter =>
            {
                counter.Emit(new Pinged("a"));
                counter.Emit(new Pinged("b"));
            });

        Assert.Equal(["first:a", "first:b", "save"], log);
    }

    [Fact]
    public async Task An_event_without_a_handler_is_dropped_and_the_save_goes_ahead()
    {
        var (log, result) = await SaveAsync(
            handlers => handlers.AddDomainEventHandler<Pinged, FirstPingedHandler>(),
            counter => counter.Emit(new Unhandled()));

        Assert.True(result.IsSuccess);
        Assert.Equal(["save"], log);
    }

    [Fact]
    public async Task The_events_are_cleared_from_the_aggregate()
    {
        var counter = new Counter();

        await SaveAsync(
            handlers => handlers.AddDomainEventHandler<Pinged, FirstPingedHandler>(),
            _ => counter.Emit(new Pinged("a")),
            counter);

        Assert.Empty(counter.DomainEvents);
    }

    // An aggregate may raise an event without changing a column, so it is tracked as Unchanged.
    [Fact]
    public async Task An_event_raised_by_an_unchanged_aggregate_is_handled()
    {
        using var provider = Build(handlers => handlers.AddDomainEventHandler<Pinged, FirstPingedHandler>());
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DispatchDbContext>();
        var counter = new Counter();
        context.Attach(counter);
        counter.Emit(new Pinged("a"));

        await UnitOfWork(scope).SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(EntityState.Unchanged, context.Entry(counter).State);
        Assert.Equal(["first:a", "save"], scope.ServiceProvider.GetRequiredService<CallLog>().Calls);
    }

    [Fact]
    public async Task An_event_raised_by_a_handler_is_handled_in_the_next_round()
    {
        var (log, result) = await SaveAsync(
            handlers => handlers.AddDomainEventHandler<Countdown, CountdownHandler>(),
            counter => counter.Emit(new Countdown(counter, 3)));

        Assert.True(result.IsSuccess);
        Assert.Equal(["countdown:3", "countdown:2", "countdown:1", "countdown:0", "save"], log);
    }

    [Fact]
    public async Task Ten_rounds_of_handlers_are_allowed()
    {
        var (log, result) = await SaveAsync(
            handlers => handlers.AddDomainEventHandler<Countdown, CountdownHandler>(),
            counter => counter.Emit(new Countdown(counter, MaxRounds - 1)));

        Assert.True(result.IsSuccess);
        Assert.Equal(MaxRounds + 1, log.Count);
        Assert.Equal("save", log[^1]);
    }

    // Handlers raising events for each other forever are a bug, not an expected failure.
    [Fact]
    public async Task An_eleventh_round_throws_and_nothing_is_saved()
    {
        using var provider = Build(handlers => handlers.AddDomainEventHandler<Countdown, CountdownHandler>());
        using var scope = provider.CreateScope();
        var counter = new Counter();
        scope.ServiceProvider.GetRequiredService<DispatchDbContext>().Add(counter);
        counter.Emit(new Countdown(counter, MaxRounds));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            UnitOfWork(scope).SaveChangesAsync(TestContext.Current.CancellationToken));

        Assert.Contains(nameof(Countdown), exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("save", scope.ServiceProvider.GetRequiredService<CallLog>().Calls);
    }

    [Fact]
    public async Task A_failing_handler_stops_dispatch_and_nothing_is_saved()
    {
        var (log, result) = await SaveAsync(
            handlers => handlers
                .AddDomainEventHandler<Pinged, FirstPingedHandler>()
                .AddDomainEventHandler<Pinged, RefusingPingedHandler>()
                .AddDomainEventHandler<Pinged, SecondPingedHandler>(),
            counter => counter.Emit(new Pinged("a")));

        Assert.Equal(Refused, result.Error);
        Assert.Equal(["first:a", "refuse:a"], log);
    }

    [Fact]
    public async Task Events_raised_by_every_tracked_aggregate_are_handled()
    {
        using var provider = Build(handlers => handlers.AddDomainEventHandler<Pinged, FirstPingedHandler>());
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DispatchDbContext>();
        var one = new Counter();
        var other = new Counter();
        context.AddRange(one, other);
        one.Emit(new Pinged("one"));
        other.Emit(new Pinged("other"));

        await UnitOfWork(scope).SaveChangesAsync(TestContext.Current.CancellationToken);

        var calls = scope.ServiceProvider.GetRequiredService<CallLog>().Calls;
        Assert.Equal(["first:one", "first:other"], calls[..^1].Order());
        Assert.Equal("save", calls[^1]);
    }

    // Each module's unit of work runs only the handlers registered for its own context.
    [Fact]
    public async Task Handlers_registered_for_another_context_are_not_run()
    {
        var (log, _) = await SaveAsync(
            handlers => handlers.AddDomainEventHandler<Pinged, FirstPingedHandler>(),
            counter => counter.Emit(new Pinged("a")),
            configure: services => services.AddHandlers<OtherDbContext>().AddDomainEventHandler<Pinged, SecondPingedHandler>());

        Assert.Equal(["first:a", "save"], log);
    }

    // Adds a counter, lets the test raise events on it, and saves through the module's unit of work.
    private static async Task<(List<string> Log, Result Result)> SaveAsync(
        Action<ModuleHandlerRegistrar<DispatchDbContext>> register,
        Action<Counter> raise,
        Counter? counter = null,
        Action<IServiceCollection>? configure = null)
    {
        using var provider = Build(register, configure);
        using var scope = provider.CreateScope();
        counter ??= new Counter();
        scope.ServiceProvider.GetRequiredService<DispatchDbContext>().Add(counter);
        raise(counter);

        var result = await UnitOfWork(scope).SaveChangesAsync(TestContext.Current.CancellationToken);

        return (scope.ServiceProvider.GetRequiredService<CallLog>().Calls, result);
    }

    private static ServiceProvider Build(
        Action<ModuleHandlerRegistrar<DispatchDbContext>> register,
        Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddScoped<CallLog>();
        services.AddDbContext<DispatchDbContext>(options => options.UseNpgsql("Host=never-connected"));
        services.AddDbContext<OtherDbContext>(options => options.UseNpgsql("Host=never-connected"));
        configure?.Invoke(services);
        register(services.AddHandlers<DispatchDbContext>());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private static IUnitOfWork UnitOfWork(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(typeof(DispatchDbContext));

    private sealed class CallLog
    {
        public List<string> Calls { get; } = [];
    }

    private sealed record Pinged(string Name) : IDomainEvent;

    private sealed record Unhandled : IDomainEvent;

    /// <summary>Counts down by raising the next event on the same aggregate until it reaches 0.</summary>
    private sealed record Countdown(Counter Counter, int Remaining) : IDomainEvent;

    private sealed class FirstPingedHandler(CallLog log) : IDomainEventHandler<Pinged>
    {
        public Task<Result> HandleAsync(Pinged domainEvent, CancellationToken cancellationToken)
        {
            log.Calls.Add($"first:{domainEvent.Name}");
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class SecondPingedHandler(CallLog log) : IDomainEventHandler<Pinged>
    {
        public Task<Result> HandleAsync(Pinged domainEvent, CancellationToken cancellationToken)
        {
            log.Calls.Add($"second:{domainEvent.Name}");
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class RefusingPingedHandler(CallLog log) : IDomainEventHandler<Pinged>
    {
        public Task<Result> HandleAsync(Pinged domainEvent, CancellationToken cancellationToken)
        {
            log.Calls.Add($"refuse:{domainEvent.Name}");
            return Task.FromResult(Result.Failure(Refused));
        }
    }

    private sealed class CountdownHandler(CallLog log) : IDomainEventHandler<Countdown>
    {
        public Task<Result> HandleAsync(Countdown domainEvent, CancellationToken cancellationToken)
        {
            log.Calls.Add($"countdown:{domainEvent.Remaining}");
            if (domainEvent.Remaining > 0)
            {
                domainEvent.Counter.Emit(domainEvent with { Remaining = domainEvent.Remaining - 1 });
            }

            return Task.FromResult(Result.Success());
        }
    }

    private readonly record struct CounterId(Guid Value) : IEntityId<CounterId>
    {
        public static CounterId New() => new(Guid.CreateVersion7());

        public static CounterId From(Guid value) => new(value);
    }

    private sealed class Counter : AggregateRoot<CounterId>
    {
        public Counter()
            : base(CounterId.New())
        {
        }

        public void Emit(IDomainEvent domainEvent) => Raise(domainEvent);
    }

    /// <summary>A module context whose save records instead of reaching a database.</summary>
    private sealed class DispatchDbContext(DbContextOptions<DispatchDbContext> options, CallLog log)
        : ModuleDbContext(options)
    {
        public DbSet<Counter> Counters => Set<Counter>();

        protected override string Schema => "dispatch";

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            log.Calls.Add("save");
            return Task.FromResult(0);
        }

        protected override void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.Entity<Counter>();
    }

    private sealed class OtherDbContext(DbContextOptions<OtherDbContext> options) : ModuleDbContext(options)
    {
        protected override string Schema => "other";

        protected override void ConfigureModel(ModelBuilder modelBuilder)
        {
        }
    }
}
