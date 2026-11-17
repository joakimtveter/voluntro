using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;

namespace Voluntro.BuildingBlocks.Infrastructure.Outbox;

/// <summary>One module's outbox processor, run on a timer by <see cref="OutboxProcessingService"/>.</summary>
public interface IOutboxProcessor
{
    /// <summary>The module context whose outbox it processes.</summary>
    Type ContextType { get; }

    /// <summary>Delivers every message that is due, batch after batch, until none are left.</summary>
    Task ProcessPendingAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Delivers the messages in <typeparamref name="TContext"/>'s outbox to every consumer of their type, in whichever
/// module it lives (ADR-0005, ADR-0014). Registered for every module context by <c>AddModuleDbContext</c>, but run
/// only by the API's <see cref="OutboxProcessingService"/>; tests call <see cref="ProcessPendingAsync"/> directly.
/// </summary>
/// <remarks>
/// <para>
/// Each consumer runs in a scope of its own, so one consumer's tracked changes never reach another's. A message is
/// processed when every consumer has handled it, now or on an earlier attempt (their inboxes skip it then). A failed
/// result or an exception from any consumer is a failed attempt for the whole message: it is retried after a backoff,
/// and after <see cref="OutboxOptions.MaxAttempts"/> it is marked failed. A message nobody consumes is processed at once.
/// </para>
/// <para>
/// Nothing claims rows: one API instance per database is assumed (ADR-0014), and the inbox keeps consumers safe if two
/// processors ever deliver the same message. Delivery order is not guaranteed.
/// </para>
/// </remarks>
public sealed class OutboxProcessor<TContext> : IOutboxProcessor
    where TContext : ModuleDbContext
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILookup<string, IntegrationEventConsumer> _consumers;
    private readonly IOptions<OutboxOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;

    internal OutboxProcessor(
        IServiceScopeFactory scopes,
        IEnumerable<IntegrationEventConsumer> consumers,
        IOptions<OutboxOptions> options,
        TimeProvider timeProvider,
        ILogger logger)
    {
        _scopes = scopes;
        _consumers = consumers.ToLookup(consumer => consumer.EventType, StringComparer.Ordinal);
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public Type ContextType => typeof(TContext);

    public async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var batchSize = _options.Value.BatchSize;

        // Only what is due when the call starts. A failed message is due again a backoff after its attempt, so later
        // than this, and the next query can't return it, however long the batches take. That is also why the loop
        // ends: every message read is processed, failed or pushed past this point, and new ones wait for the next call.
        var dueBy = _timeProvider.GetUtcNow();

        while (true)
        {
            await using var scope = _scopes.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>();

            var due = await context.Set<OutboxMessage>()
                .Where(message => message.ProcessedAt == null && message.FailedAt == null && message.NextAttemptAt <= dueBy)
                .OrderBy(message => message.OccurredAt)
                .ThenBy(message => message.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            foreach (var message in due)
            {
                await DeliverAsync(message, cancellationToken);

                // Saved one by one, so what was delivered stays recorded if a later message stops the batch.
                await context.SaveChangesAsync(cancellationToken);
            }

            if (due.Count < batchSize)
            {
                return;
            }
        }
    }

    private async Task DeliverAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var attempt = message.Attempts + 1;
        var failed = false;

        foreach (var consumer in _consumers[message.Type])
        {
            using var activity = OutboxTelemetry.StartConsume(message, consumer);
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var result = await consumer.ConsumeAsync(scope.ServiceProvider, message.Id, message.Payload, cancellationToken);
                if (result.IsFailure)
                {
                    failed = true;
                    activity?.SetStatus(ActivityStatusCode.Error, result.Error.Code);
                    OutboxLog.AttemptFailed(_logger, message.Id, message.Type, consumer.Name, attempt, result.Error.Code);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The host is stopping: not the consumer's fault, so not an attempt. The message is retried on the next
                // start.
                throw;
            }
#pragma warning disable CA1031 // Any exception from a consumer is a failed attempt, retried later; it must not stop the others.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                failed = true;
                activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
                OutboxLog.AttemptThrew(_logger, message.Id, message.Type, consumer.Name, attempt, exception);
            }
        }

        var now = _timeProvider.GetUtcNow();
        if (!failed)
        {
            message.MarkProcessed(now);
        }
        else if (message.RecordFailedAttempt(now, _options.Value.MaxAttempts))
        {
            OutboxLog.GaveUp(_logger, message.Id, message.Type, message.Attempts);
        }
    }
}
