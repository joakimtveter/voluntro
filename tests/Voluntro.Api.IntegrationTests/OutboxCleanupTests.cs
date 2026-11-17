using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Voluntro.Api.IntegrationTests.Database;
using Voluntro.Api.IntegrationTests.Inbound;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.Api.IntegrationTests.Sample.Persistence;
using Voluntro.BuildingBlocks.Infrastructure.Outbox;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// The cleanup against a real Postgres, with a fake clock: outbox messages processed or failed more than the retention
/// period (7 days) ago, and inbox records that old, are deleted in every module's schema; pending messages and their
/// inbox records never are.
/// </summary>
public sealed class OutboxCleanupTests(PostgresDatabase database) : DatabaseTests(database)
{
    private static readonly TimeSpan Retention = TimeSpan.FromDays(7);

    // Acceptance criterion: processed messages older than the retention period are removed by the cleanup job.
    [Fact]
    public async Task Processed_messages_and_inbox_records_are_removed_once_older_than_the_retention_period()
    {
        var clock = NewClock();
        using var api = WithClock(clock);
        await PlaceOrderAsync(api, "Kari Nordmann");
        await ProcessAsync(api);

        // Exactly the retention period: kept.
        clock.Advance(Retention);
        await CleanUpAsync(api);
        Assert.Equal(1, await CountAsync("sample.outbox"));
        Assert.Equal(2, await CountAsync("inbound.inbox"));

        clock.Advance(TimeSpan.FromSeconds(1));
        await CleanUpAsync(api);
        Assert.Equal(0, await CountAsync("sample.outbox"));
        Assert.Equal(0, await CountAsync("inbound.inbox"));

        // Only the outbox's own bookkeeping goes: what the consumer did stays.
        Assert.Equal(1, await CountAsync("inbound.received_orders"));
    }

    // Payloads can carry one-time tokens (ADR-0009), so failed messages don't stay forever either.
    [Fact]
    public async Task Failed_messages_are_removed_once_older_than_the_retention_period()
    {
        var clock = NewClock();
        using var api = WithClock(clock, builder => builder.UseSetting("Outbox:MaxAttempts", "1"));
        await PlaceOrderAsync(api, InboundFlags.Refuse);
        await ProcessAsync(api);
        Assert.Equal(1, await CountAsync("sample.outbox WHERE failed_at IS NOT NULL"));

        clock.Advance(Retention);
        await CleanUpAsync(api);
        Assert.Equal(1, await CountAsync("sample.outbox"));

        clock.Advance(TimeSpan.FromSeconds(1));
        await CleanUpAsync(api);
        Assert.Equal(0, await CountAsync("sample.outbox"));
    }

    [Fact]
    public async Task A_pending_message_is_never_removed_however_old()
    {
        var clock = NewClock();
        using var api = WithClock(clock);
        await PlaceOrderAsync(api, InboundFlags.Refuse);
        await ProcessAsync(api);

        clock.Advance(TimeSpan.FromDays(30));
        await CleanUpAsync(api);

        Assert.Equal(1, await CountAsync("sample.outbox WHERE processed_at IS NULL AND failed_at IS NULL"));
    }

    // The recording consumer handles the message and the refusing one fails, so it stays pending. Then the API is
    // off for longer than the retention period. If the cleanup deleted the recording consumer's inbox record by age
    // alone, the redelivery would record the order a second time.
    [Fact]
    public async Task An_inbox_record_is_kept_while_its_message_is_pending_however_old()
    {
        var clock = NewClock();
        using var api = WithClock(clock, builder => builder.UseSetting("Outbox:MaxAttempts", "2"));
        await PlaceOrderAsync(api, InboundFlags.Refuse);
        await ProcessAsync(api);
        Assert.Equal(1, await CountAsync("inbound.inbox"));

        clock.Advance(TimeSpan.FromDays(30));
        await CleanUpAsync(api);
        Assert.Equal(1, await CountAsync("inbound.inbox"));

        // The second and last attempt: the recording consumer skips it through its inbox, and the message fails.
        await ProcessAsync(api);
        Assert.Equal(1, await CountAsync("sample.outbox WHERE failed_at IS NOT NULL"));
        Assert.Equal(1, await CountAsync("inbound.received_orders"));

        // Finished, so its 30-day-old inbox record goes; the message itself failed just now and stays.
        await CleanUpAsync(api);
        Assert.Equal(0, await CountAsync("inbound.inbox"));
        Assert.Equal(1, await CountAsync("sample.outbox"));
    }

    // The retention period is a setting like the others.
    [Fact]
    public async Task The_retention_period_comes_from_the_settings()
    {
        var clock = NewClock();
        using var api = WithClock(clock, builder => builder.UseSetting("Outbox:RetentionPeriod", "01:00:00"));
        await PlaceOrderAsync(api, "Kari Nordmann");
        await ProcessAsync(api);

        clock.Advance(TimeSpan.FromHours(1) + TimeSpan.FromSeconds(1));
        await CleanUpAsync(api);

        Assert.Equal(0, await CountAsync("sample.outbox"));
    }

    // Starts on a whole microsecond, Postgres' precision, so the boundary comparisons are exact.
    private static FakeTimeProvider NewClock()
    {
        var now = DateTimeOffset.UtcNow;
        return new FakeTimeProvider(now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond)));
    }

    private WebApplicationFactory<Program> WithClock(TimeProvider clock, Action<IWebHostBuilder>? configure = null) =>
        Api.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => services.AddSingleton(clock));
            configure?.Invoke(builder);
        });

    private static Task ProcessAsync(WebApplicationFactory<Program> api) =>
        api.Services.GetRequiredService<OutboxProcessor<SampleDbContext>>().ProcessPendingAsync(TestContext.Current.CancellationToken);

    private static Task CleanUpAsync(WebApplicationFactory<Program> api) =>
        api.Services.GetRequiredService<OutboxCleanup>().CleanUpAsync(TestContext.Current.CancellationToken);

    private async Task PlaceOrderAsync(WebApplicationFactory<Program> api, string customerName)
    {
        var order = SampleOrder.Create(customerName, note: null);
        order.AddLine("Hymn book", 1);
        await SaveAsync<SampleDbContext>(order);

        using var client = api.CreateClient();
        using var response = await client.PostAsync($"/api/sample/orders/{order.Id}/place", content: null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // The tables are named by the tests themselves, never by input.
    private async Task<long> CountAsync(string tableAndFilter)
    {
        await using var command = Api.Services.GetRequiredService<NpgsqlDataSource>()
            .CreateCommand($"SELECT count(*) FROM {tableAndFilter}");
        return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }
}
