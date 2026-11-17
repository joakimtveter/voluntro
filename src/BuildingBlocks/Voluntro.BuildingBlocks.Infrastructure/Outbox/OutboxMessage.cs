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

    /// <summary>When the publishing command saved (UTC). Stamped by the audit interceptor, from its <see cref="TimeProvider"/>.</summary>
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

    /// <summary>The longest wait between two attempts.</summary>
    public static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);

    /// <summary>Stamps a new message with the time its command is saved; it is due straight away.</summary>
    public void Stamp(DateTimeOffset now)
    {
        OccurredAt = now;
        NextAttemptAt = now;
    }

    /// <summary>Every consumer has handled it.</summary>
    public void MarkProcessed(DateTimeOffset now) => ProcessedAt = now;

    /// <summary>
    /// Counts a failed attempt. After <paramref name="maxAttempts"/> the message is failed and never retried; before
    /// that it is due again after <see cref="Backoff"/>.
    /// </summary>
    /// <returns>True when this attempt was the last one and the message is now failed.</returns>
    public bool RecordFailedAttempt(DateTimeOffset now, int maxAttempts)
    {
        Attempts++;
        if (Attempts >= maxAttempts)
        {
            FailedAt = now;
            return true;
        }

        NextAttemptAt = now + Backoff(Attempts);
        return false;
    }

    /// <summary>
    /// How long to wait after the given number of failed attempts: 2, 4, 8 … seconds, at most <see cref="MaxBackoff"/>.
    /// </summary>
    public static TimeSpan Backoff(int attempts) =>
        TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempts), MaxBackoff.TotalSeconds));
}
