using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;

namespace Voluntro.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Deletes what the outbox no longer needs, in every module's schema (ADR-0005, ADR-0014): outbox messages processed or
/// failed more than <see cref="OutboxOptions.RetentionPeriod"/> ago, and inbox records older than that. Payloads can
/// carry one-time tokens (ADR-0009), so failed messages go too; the Error logged when they failed remains. A message
/// that is still pending is never deleted, however old.
/// </summary>
/// <remarks>
/// Inbox records only need to outlive redeliveries of their message, which end when it is processed or failed: within
/// minutes of being published, far inside the retention period. Run hourly by <see cref="OutboxCleanupService"/>; tests
/// call <see cref="CleanUpAsync"/> directly.
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
        foreach (var contextType in processors.Select(processor => processor.ContextType))
        {
            await using var scope = scopes.CreateAsyncScope();
            var context = (ModuleDbContext)scope.ServiceProvider.GetRequiredService(contextType);

            var outbox = await context.Set<OutboxMessage>()
                .Where(message => message.ProcessedAt < cutoff || message.FailedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);
            var inbox = await context.Set<InboxMessage>()
                .Where(message => message.ProcessedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            if (outbox > 0 || inbox > 0)
            {
                OutboxLog.CleanedUp(logger, contextType.Name, outbox, inbox, cutoff);
            }
        }
    }
}
