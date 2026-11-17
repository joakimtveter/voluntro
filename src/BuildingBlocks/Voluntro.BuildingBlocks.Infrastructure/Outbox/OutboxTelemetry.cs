using System.Diagnostics;

namespace Voluntro.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Traces delivery: one activity per message and consumer, whose parent is the activity that published the message,
/// so in the dashboard a consumer shows up inside the trace of the request that caused it (decided over a span link).
/// </summary>
public static class OutboxTelemetry
{
    /// <summary>The <see cref="ActivitySource"/> name; ServiceDefaults adds it to tracing.</summary>
    public const string SourceName = "Voluntro.Outbox";

    private static readonly ActivitySource Source = new(SourceName);

    internal static Activity? StartConsume(OutboxMessage message, IntegrationEventConsumer consumer)
    {
        // A message published outside any activity, or with an unreadable trace, starts a trace of its own.
        ActivityContext.TryParse(message.TraceParent, traceState: null, out var parent);

        var activity = Source.StartActivity($"{message.Type} consume", ActivityKind.Consumer, parent);
        activity?.SetTag("messaging.message.id", message.Id);
        activity?.SetTag("messaging.destination.name", message.Type);
        activity?.SetTag("voluntro.outbox.consumer", consumer.Name);
        activity?.SetTag("voluntro.outbox.attempt", message.Attempts + 1);
        return activity;
    }
}
