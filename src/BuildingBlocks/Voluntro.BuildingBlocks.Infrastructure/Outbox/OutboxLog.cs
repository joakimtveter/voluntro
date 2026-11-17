using Microsoft.Extensions.Logging;

namespace Voluntro.BuildingBlocks.Infrastructure.Outbox;

/// <summary>The outbox's log messages: IDs, types and error codes only, never payloads (they can hold personal data).</summary>
internal static partial class OutboxLog
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Delivering outbox message {MessageId} ({EventType}) to {Consumer} failed on attempt {Attempt}: {ErrorCode}.")]
    public static partial void AttemptFailed(
        ILogger logger, Guid messageId, string eventType, string consumer, int attempt, string errorCode);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Delivering outbox message {MessageId} ({EventType}) to {Consumer} threw on attempt {Attempt}.")]
    public static partial void AttemptThrew(
        ILogger logger, Guid messageId, string eventType, string consumer, int attempt, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Gave up delivering outbox message {MessageId} ({EventType}) after {Attempts} attempts; it is marked failed and won't be retried.")]
    public static partial void GaveUp(ILogger logger, Guid messageId, string eventType, int attempts);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Processing the outbox of {Context} failed; it is tried again on the next tick.")]
    public static partial void ProcessingFailed(ILogger logger, string context, Exception exception);
}
