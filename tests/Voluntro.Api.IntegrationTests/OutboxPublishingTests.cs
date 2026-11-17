using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Voluntro.Api.IntegrationTests.Database;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.Api.IntegrationTests.Sample.Contracts;
using Voluntro.Api.IntegrationTests.Sample.Persistence;
using Voluntro.BuildingBlocks.Application;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// Publishing against a real Postgres: an integration event is written to the publishing module's outbox in the same
/// save as the change that published it, or not at all (ADR-0005). Placing a sample order publishes
/// <see cref="OrderPlaced"/> from a domain event handler. The outbox row type is internal, so rows are read with SQL.
/// </summary>
public sealed class OutboxPublishingTests(PostgresDatabase database) : DatabaseTests(database)
{
    // Postgres keeps microseconds; a .NET timestamp has ten ticks to the microsecond.
    private static readonly TimeSpan Precision = TimeSpan.FromMicroseconds(1);

    [Fact]
    public async Task A_successful_command_writes_its_integration_event_to_the_outbox()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = SampleOrder.Create("Kari Nordmann", note: null);
        order.AddLine("Hymn book", 3);
        await SaveAsync<SampleDbContext>(order);
        using var client = Api.CreateClient();
        var before = DateTimeOffset.UtcNow;

        using var response = await client.PostAsync($"/api/sample/orders/{order.Id}/place", content: null, ct);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var message = Assert.Single(await ReadOutboxAsync());
        Assert.Equal(7, message.Id.Version);
        Assert.Equal(OrderPlaced.EventType, message.Type);
        Assert.InRange(message.OccurredAt, before - Precision, after + Precision);
        Assert.Equal(message.OccurredAt, message.NextAttemptAt);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.ProcessedAt);
        Assert.Null(message.FailedAt);

        using var payload = JsonDocument.Parse(message.Payload);
        Assert.Equal(order.Id.Value, payload.RootElement.GetProperty("orderId").GetGuid());
        Assert.Equal("Kari Nordmann", payload.RootElement.GetProperty("customerName").GetString());
    }

    // The pipeline is the supported way to save, but a direct save (a test, a hand-written job) must not write a
    // message with no time: the audit interceptor stamps it, in every save.
    [Fact]
    public async Task A_message_saved_directly_on_the_context_is_stamped_too()
    {
        var ct = TestContext.Current.CancellationToken;
        var before = DateTimeOffset.UtcNow;

        await using (var scope = CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
            ((IOutbox)context).Add(new OrderPlaced(Guid.CreateVersion7(), "Kari Nordmann"));
            await context.SaveChangesAsync(ct);
        }

        var after = DateTimeOffset.UtcNow;
        var message = Assert.Single(await ReadOutboxAsync());
        Assert.InRange(message.OccurredAt, before - Precision, after + Precision);
        Assert.Equal(message.OccurredAt, message.NextAttemptAt);
    }

    // Acceptance criterion: if the originating transaction rolls back, no integration event is published. The event
    // is already in the change tracker when the second handler fails (SampleModule registers the publisher first).
    [Fact]
    public async Task A_command_that_fails_after_publishing_writes_nothing_to_the_outbox()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = SampleOrder.Create("Kari Nordmann", note: null, SampleCustomerId.New());
        order.AddLine("Hymn book", 3);
        await SaveAsync<SampleDbContext>(order);
        using var client = Api.CreateClient();

        using var response = await client.PostAsync($"/api/sample/orders/{order.Id}/place", content: null, ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await ReadOutboxAsync());
    }

    // The trace is what links delivery to the request in the dashboard; the test server runs with an activity.
    [Fact]
    public async Task The_message_carries_the_requests_trace()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = SampleOrder.Create("Kari Nordmann", note: null);
        order.AddLine("Hymn book", 3);
        await SaveAsync<SampleDbContext>(order);
        using var client = Api.CreateClient();

        using var response = await client.PostAsync($"/api/sample/orders/{order.Id}/place", content: null, ct);

        var traceId = Assert.Single(response.Headers.GetValues("X-Trace-Id"));
        var message = Assert.Single(await ReadOutboxAsync());
        Assert.StartsWith($"00-{traceId}-", message.TraceParent, StringComparison.Ordinal);
    }

    private async Task<List<OutboxRow>> ReadOutboxAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var command = Api.Services.GetRequiredService<NpgsqlDataSource>().CreateCommand("""
            SELECT id, type, payload::text, trace_parent, occurred_at, attempts, next_attempt_at, processed_at, failed_at
            FROM sample.outbox
            """);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var rows = new List<OutboxRow>();
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new OutboxRow(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                await reader.IsDBNullAsync(3, ct) ? null : reader.GetString(3),
                reader.GetFieldValue<DateTimeOffset>(4),
                reader.GetInt32(5),
                reader.GetFieldValue<DateTimeOffset>(6),
                await reader.IsDBNullAsync(7, ct) ? null : reader.GetFieldValue<DateTimeOffset>(7),
                await reader.IsDBNullAsync(8, ct) ? null : reader.GetFieldValue<DateTimeOffset>(8)));
        }

        return rows;
    }

    private sealed record OutboxRow(
        Guid Id,
        string Type,
        string Payload,
        string? TraceParent,
        DateTimeOffset OccurredAt,
        int Attempts,
        DateTimeOffset NextAttemptAt,
        DateTimeOffset? ProcessedAt,
        DateTimeOffset? FailedAt);
}
