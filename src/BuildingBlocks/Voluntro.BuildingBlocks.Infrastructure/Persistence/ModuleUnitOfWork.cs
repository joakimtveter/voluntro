using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Infrastructure.Outbox;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// A module's <see cref="IUnitOfWork"/>: the one place a command's changes are saved (ADR-0012). Before the save it
/// runs the handlers of the domain events the tracked aggregates have raised, round after round while handlers raise
/// more (ADR-0013). Then it stamps the outbox messages they and the command published with the time of the save.
/// Everything is saved together, and the audit interceptor stamps every aggregate that changed.
/// </summary>
/// <remarks>
/// Registered by the handler registrar as the module's <see cref="IUnitOfWork"/>, keyed by the context type. A direct
/// <c>SaveChangesAsync</c> on the context dispatches and stamps nothing: the pipeline is the only supported way to
/// save. A DbContext can't do this itself: it is built from its options alone and can't see the scope's handlers or
/// clock.
/// </remarks>
internal sealed class ModuleUnitOfWork<TContext>(
    TContext context,
    DomainEventHandlers<TContext> domainEventHandlers,
    TimeProvider timeProvider,
    IServiceProvider services) : IUnitOfWork
    where TContext : ModuleDbContext
{
    /// <summary>
    /// How many rounds of handlers may run before the save. Events still raised after that are a bug (handlers raising
    /// events for each other forever), and the save fails with an exception.
    /// </summary>
    internal const int MaxDispatchRounds = 10;

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var dispatched = await DispatchDomainEventsAsync(cancellationToken);
        if (dispatched.IsFailure)
        {
            return dispatched;
        }

        StampNewOutboxMessages();

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // Usually a row's xmin no longer matched: someone else saved the aggregate after it was loaded
            // (ADR-0011). A bug that updates or deletes a row that doesn't exist fails the same way, so the rows are
            // logged to tell the two apart.
            var logger = context.GetService<ILoggerFactory>().CreateLogger(context.GetType());
            if (logger.IsEnabled(LogLevel.Information))
            {
                var rows = DescribeRows(exception.Entries);
                PersistenceLog.ConcurrencyConflict(logger, context.GetType().Name, rows, exception);
            }

            return CommonErrors.ConcurrencyConflict;
        }
    }

    public void DiscardChanges() => context.ChangeTracker.Clear();

    private async Task<Result> DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        for (var round = 1; ; round++)
        {
            var domainEvents = TakeDomainEvents();
            if (domainEvents.Count == 0)
            {
                return Result.Success();
            }

            if (round > MaxDispatchRounds)
            {
                var names = string.Join(", ", domainEvents.Select(domainEvent => domainEvent.GetType().Name).Distinct());
                throw new InvalidOperationException(
                    $"{typeof(TContext).Name}: domain events were still being raised after {MaxDispatchRounds} rounds of " +
                    $"handlers ({names}). Handlers that raise events for each other never finish.");
            }

            foreach (var domainEvent in domainEvents)
            {
                foreach (var handle in domainEventHandlers.For(domainEvent.GetType()))
                {
                    var result = await handle(services, domainEvent, cancellationToken);
                    if (result.IsFailure)
                    {
                        return result;
                    }
                }
            }
        }
    }

    /// <summary>
    /// The events raised by every tracked aggregate, whatever its state (an aggregate may raise an event without
    /// changing a column), cleared from the aggregates so the next round sees only new ones.
    /// </summary>
    private List<IDomainEvent> TakeDomainEvents()
    {
        var domainEvents = new List<IDomainEvent>();

        // Entries() detects changes first, so aggregates added since the last round are included.
        foreach (var entry in context.ChangeTracker.Entries<IHasDomainEvents>())
        {
            var aggregate = entry.Entity;
            if (aggregate.DomainEvents.Count == 0)
            {
                continue;
            }

            // Copied before clearing: DomainEvents is a live view of the aggregate's list.
            domainEvents.AddRange(aggregate.DomainEvents);
            aggregate.ClearDomainEvents();
        }

        return domainEvents;
    }

    // After dispatch, so events published by domain event handlers are stamped too.
    private void StampNewOutboxMessages()
    {
        var now = timeProvider.GetUtcNow();
        foreach (var entry in context.ChangeTracker.Entries<OutboxMessage>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.Stamp(now);
            }
        }
    }

    // "SampleOrder 0193…, SampleOrderLine 0193…": IDs only, never property values.
    private static string DescribeRows(IEnumerable<EntityEntry> entries) =>
        string.Join(", ", entries.Select(entry =>
            $"{entry.Metadata.ClrType.Name} " +
            string.Join("/", entry.Metadata.FindPrimaryKey()?.Properties.Select(key => entry.Property(key.Name).CurrentValue) ?? [])));
}
