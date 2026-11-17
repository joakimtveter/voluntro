namespace Voluntro.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// A row in a module's <c>inbox</c> table: a consumer in this module has handled this message, so a redelivery is
/// skipped (ADR-0005). Saved in the same save as the consumer's own changes.
/// </summary>
internal sealed class InboxMessage
{
    /// <summary>The outbox message's ID, from the publishing module.</summary>
    public required Guid MessageId { get; init; }

    /// <summary>The consumer that handled it: its handler's full type name (ADR-0014).</summary>
    public required string Consumer { get; init; }

    /// <summary>When the consumer handled it (UTC); rows are deleted after the retention period.</summary>
    public required DateTimeOffset ProcessedAt { get; init; }
}
