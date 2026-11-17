using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Contracts;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// One registered consumer of an integration event: what it consumes, its name in the inbox, and how to run it.
/// Every module's registrations are singletons in the container; together they are the registry of event types, and
/// each outbox processor delivers to every consumer of a message's type, in whichever module it lives.
/// </summary>
internal sealed class IntegrationEventConsumer
{
    private readonly Func<IServiceProvider, Guid, string, CancellationToken, Task<Result>> _consume;

    private IntegrationEventConsumer(
        string eventType,
        string name,
        Func<IServiceProvider, Guid, string, CancellationToken, Task<Result>> consume)
    {
        EventType = eventType;
        Name = name;
        _consume = consume;
    }

    /// <summary>The event type it consumes, e.g. <c>identity.user_registered.v1</c>.</summary>
    public string EventType { get; }

    /// <summary>Its name in the inbox: the handler's full type name (ADR-0014).</summary>
    public string Name { get; }

    /// <summary>
    /// Runs the consumer for one message in <paramref name="scope"/>, a scope of its own: Inbox → Transaction → handler.
    /// </summary>
    public Task<Result> ConsumeAsync(IServiceProvider scope, Guid messageId, string payload, CancellationToken cancellationToken) =>
        _consume(scope, messageId, payload, cancellationToken);

    /// <summary>
    /// The consumer for <typeparamref name="THandler"/>, saving through <typeparamref name="TContext"/>'s unit of work.
    /// <typeparamref name="TEvent"/> is known here, so the payload is deserialized without reflection.
    /// </summary>
    public static IntegrationEventConsumer For<TContext, TEvent, THandler>(object handlerKey)
        where TContext : ModuleDbContext
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        var name = typeof(THandler).FullName!;

        return new IntegrationEventConsumer(TEvent.EventType, name, async (scope, messageId, payload, cancellationToken) =>
        {
            var context = scope.GetRequiredService<TContext>();
            var inbox = context.Set<InboxMessage>();

            // Inbox: handled before, so this is a redelivery.
            if (await inbox.AnyAsync(m => m.MessageId == messageId && m.Consumer == name, cancellationToken))
            {
                return Result.Success();
            }

            var integrationEvent = JsonSerializer.Deserialize<TEvent>(payload, OutboxJson.Options)
                ?? throw new InvalidOperationException($"Outbox message {messageId} has an empty {TEvent.EventType} payload.");

            // Transaction: the handler's changes and the inbox record are saved together, or neither is. Saving through
            // the unit of work also dispatches the consumer module's own domain events.
            var unitOfWork = scope.GetRequiredKeyedService<IUnitOfWork>(typeof(TContext));
            var handler = scope.GetRequiredKeyedService<THandler>(handlerKey);

            var result = await handler.HandleAsync(integrationEvent, cancellationToken);
            if (result.IsFailure)
            {
                unitOfWork.DiscardChanges();
                return result;
            }

            inbox.Add(new InboxMessage
            {
                MessageId = messageId,
                Consumer = name,
                ProcessedAt = scope.GetRequiredService<TimeProvider>().GetUtcNow(),
            });

            var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
            if (saved.IsFailure)
            {
                unitOfWork.DiscardChanges();
            }

            return saved;
        });
    }
}
