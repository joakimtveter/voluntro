namespace Voluntro.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// How the outbox is processed, from the configuration section <c>Outbox</c>. Registered and validated at startup by
/// <see cref="OutboxServiceCollectionExtensions.AddOutboxProcessing"/>; the defaults are the agreed values (ADR-0014).
/// </summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>Failed attempts before a message is marked failed and no longer retried. With the backoff, 10 is about 15 minutes.</summary>
    public int MaxAttempts { get; set; } = 10;

    /// <summary>How long processed and failed messages, and inbox records, are kept.</summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>How often each module's outbox is checked for due messages.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>How many messages are read at a time; the processor keeps reading while batches come back full.</summary>
    public int BatchSize { get; set; } = 20;

    internal IEnumerable<string> Validate()
    {
        if (MaxAttempts < 1)
        {
            yield return $"{SectionName}:{nameof(MaxAttempts)} must be at least 1.";
        }

        if (RetentionPeriod <= TimeSpan.Zero)
        {
            yield return $"{SectionName}:{nameof(RetentionPeriod)} must be positive.";
        }

        if (PollingInterval <= TimeSpan.Zero)
        {
            yield return $"{SectionName}:{nameof(PollingInterval)} must be positive.";
        }

        if (BatchSize < 1)
        {
            yield return $"{SectionName}:{nameof(BatchSize)} must be at least 1.";
        }
    }
}
