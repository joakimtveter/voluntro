using System.Diagnostics;
using Voluntro.BuildingBlocks.Contracts;

namespace Voluntro.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// A row in a module's <c>outbox</c> table: one integration event waiting to be delivered, or already delivered or
/// given up on (ADR-0005, ADR-0014). Added by <c>IOutbox.Add</c> and saved with the command that published it.
/// </summary>
/// <remarks>
/// Holds no error text: an exception's message can contain personal data, so failures go to the log only.
/// </remarks>
internal sealed class OutboxMessage
{
    /// <summary>The message ID (UUIDv7), which consumers' inboxes record.</summary>
    public required Guid Id { get; init; }

    /// <summary>The event's <see cref="IIntegrationEvent.EventType"/>, e.g. <c>identity.user_registered.v1</c>.</summary>
    public required string Type { get; init; }

    /// <summary>The event as JSON (<c>jsonb</c>), written with <see cref="OutboxJson.Options"/>.</summary>
    public required string Payload { get; init; }

    /// <summary>
    /// The W3C <c>traceparent</c> of the activity that published the event, so delivery shows up in the same trace.
    /// </summary>
    public string? TraceParent { get; init; }

    /// <summary>When the publishing command saved (UTC). Stamped by the unit of work, from its <see cref="TimeProvider"/>.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Failed delivery attempts so far.</summary>
    public int Attempts { get; private set; }

    /// <summary>When the message is next due: <see cref="OccurredAt"/> at first, later pushed back after each failure.</summary>
    public DateTimeOffset NextAttemptAt { get; private set; }

    /// <summary>When every consumer had handled it.</summary>
    public DateTimeOffset? ProcessedAt { get; private set; }

    /// <summary>When delivery was given up after the last allowed attempt.</summary>
    public DateTimeOffset? FailedAt { get; private set; }

    public static OutboxMessage Create<TEvent>(TEvent integrationEvent)
        where TEvent : IIntegrationEvent =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Type = TEvent.EventType,
            Payload = OutboxJson.Serialize(integrationEvent),
            TraceParent = Activity.Current?.Id,
        };

    /// <summary>Stamps a new message with the time its command is saved; it is due straight away.</summary>
    public void Stamp(DateTimeOffset now)
    {
        OccurredAt = now;
        NextAttemptAt = now;
    }
}
