using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;

namespace Voluntro.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Deletes what the outbox no longer needs, in every module's schema (ADR-0005, ADR-0014): outbox messages processed or
/// failed more than <see cref="OutboxOptions.RetentionPeriod"/> ago, and inbox records older than that whose message is
/// no longer pending. Payloads can carry one-time tokens (ADR-0009), so failed messages go too; the Error logged when
/// they failed remains. A message that is still pending is never deleted, however old.
/// </summary>
/// <remarks>
/// An inbox record is what stops a consumer that already handled a message from handling it again, so it is kept for
/// as long as its message can be redelivered: while the message is pending in any module's outbox. Its age alone isn't
/// enough. A message is usually finished within minutes, but one still being retried after the API was stopped for
/// longer than the retention period, or with a high <see cref="OutboxOptions.MaxAttempts"/>, would otherwise reach a
/// consumer whose record was just deleted. Run hourly by <see cref="OutboxCleanupService"/>; tests call
/// <see cref="CleanUpAsync"/> directly.
/// </remarks>
public sealed class OutboxCleanup(
    IServiceScopeFactory scopes,
    IEnumerable<IOutboxProcessor> processors,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxCleanup> logger)
{
    public async Task CleanUpAsync(CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow() - options.Value.RetentionPeriod;

        // Every module context has an outbox processor, so their context types are every module's outbox and inbox.
        var contextTypes = processors.Select(processor => processor.ContextType).ToList();

        await using var scope = scopes.CreateAsyncScope();
        var contexts = contextTypes
            .Select(contextType => (ModuleDbContext)scope.ServiceProvider.GetRequiredService(contextType))
            .ToList();

        // Read before anything is deleted. A message published after this has no inbox record old enough to delete,
        // and a pending message never becomes pending again once it is finished, so the list can't go stale in a way
        // that matters.
        var pending = await PendingMessageIdsAsync(contexts, cancellationToken);

        foreach (var context in contexts)
        {
            var outbox = await context.Set<OutboxMessage>()
                .Where(message => message.ProcessedAt < cutoff || message.FailedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);
            var inbox = await context.Set<InboxMessage>()
                .Where(message => message.ProcessedAt < cutoff && !pending.Contains(message.MessageId))
                .ExecuteDeleteAsync(cancellationToken);

            if (outbox > 0 || inbox > 0)
            {
                OutboxLog.CleanedUp(logger, context.GetType().Name, outbox, inbox, cutoff);
            }
        }
    }

    // A consumer's inbox may hold messages from any module's outbox, so every outbox is read. Pending messages are
    // the few the processor's partial index covers.
    private static async Task<List<Guid>> PendingMessageIdsAsync(
        IEnumerable<ModuleDbContext> contexts,
        CancellationToken cancellationToken)
    {
        var pending = new List<Guid>();
        foreach (var context in contexts)
        {
            pending.AddRange(await context.Set<OutboxMessage>()
                .Where(message => message.ProcessedAt == null && message.FailedAt == null)
                .Select(message => message.Id)
                .ToListAsync(cancellationToken));
        }

        return pending;
    }
}
