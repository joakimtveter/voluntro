using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Voluntro.Api.IntegrationTests.Database;
using Voluntro.Api.IntegrationTests.Inbound;
using Voluntro.Api.IntegrationTests.Inbound.Persistence;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.Api.IntegrationTests.Sample.Persistence;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Contracts;
using Voluntro.BuildingBlocks.Infrastructure.Outbox;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// Delivery against a real Postgres: the sample module (A) publishes <c>sample.order_placed.v1</c> when an order is
/// placed, and the inbound module (B) consumes it with two handlers: one records the order, the other fails on demand
/// (ADR-0005, ADR-0014). Tests deliver by calling the sample module's processor; nothing runs in the background.
/// </summary>
public sealed class OutboxDeliveryTests(PostgresDatabase database) : DatabaseTests(database)
{
    private static readonly string Recorder = typeof(RecordReceivedOrderHandler).FullName!;
    private static readonly string Refuser = typeof(RefuseFlaggedOrderHandler).FullName!;

    // Acceptance criterion: an event published by sample module A is consumed by sample module B.
    [Fact]
    public async Task An_event_published_by_one_module_is_consumed_by_another()
    {
        var orderId = await PlaceOrderAsync(Api, "Kari Nordmann");

        await ProcessAsync(Api);

        var received = Assert.Single(await ReadReceivedOrdersAsync());
        Assert.Equal(orderId.Value, received.OrderId);
        Assert.Equal("Kari Nordmann", received.CustomerName);

        var message = Assert.Single(await ReadOutboxAsync());
        Assert.NotNull(message.ProcessedAt);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.FailedAt);

        // Each consumer recorded the message in the consumer module's own inbox.
        Assert.Equal([(message.Id, Recorder), (message.Id, Refuser)], (await ReadInboxAsync()).Order());
    }

    // Acceptance criterion: delivering the same message twice results in a single effect.
    [Fact]
    public async Task Delivering_the_same_message_twice_has_a_single_effect()
    {
        await PlaceOrderAsync(Api, "Kari Nordmann");
        await ProcessAsync(Api);

        // As if the processor had stopped after the consumers saved but before it marked the message processed.
        await ExecuteAsync("UPDATE sample.outbox SET processed_at = NULL");
        await ProcessAsync(Api);

        Assert.Single(await ReadReceivedOrdersAsync());
        Assert.NotNull(Assert.Single(await ReadOutboxAsync()).ProcessedAt);
        Assert.Equal(2, (await ReadInboxAsync()).Count);
    }

    [Fact]
    public async Task A_message_nobody_consumes_is_processed_straight_away()
    {
        await using (var scope = CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ISampleDbContext>().Add(new NobodyListens());
            var unitOfWork = scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(typeof(SampleDbContext));
            await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await ProcessAsync(Api);

        var message = Assert.Single(await ReadOutboxAsync());
        Assert.Equal(NobodyListens.EventType, message.Type);
        Assert.NotNull(message.ProcessedAt);
        Assert.Empty(await ReadInboxAsync());
    }

    // The processor keeps reading while batches come back full, so one call delivers everything that is due.
    [Fact]
    public async Task One_call_delivers_every_due_message_batch_after_batch()
    {
        using var api = Api.WithWebHostBuilder(builder => builder.UseSetting("Outbox:BatchSize", "2"));
        for (var i = 0; i < 5; i++)
        {
            await PlaceOrderAsync(api, $"Customer {i}");
        }

        await ProcessAsync(api);

        Assert.Equal(5, (await ReadReceivedOrdersAsync()).Count);
        Assert.All(await ReadOutboxAsync(), message => Assert.NotNull(message.ProcessedAt));
    }

    // One consumer fails, the other succeeds: the message is retried, and the inbox makes the successful consumer skip
    // it, so it still has one effect.
    [Fact]
    public async Task A_failed_attempt_is_retried_after_a_backoff_and_the_consumers_that_succeeded_skip_it()
    {
        var clock = NewClock();
        using var api = WithClock(Api, clock);
        await PlaceOrderAsync(api, InboundFlags.Refuse);

        await ProcessAsync(api);

        var message = Assert.Single(await ReadOutboxAsync());
        Assert.Equal(1, message.Attempts);
        Assert.Equal(clock.GetUtcNow() + TimeSpan.FromSeconds(2), message.NextAttemptAt);
        Assert.Null(message.ProcessedAt);
        Assert.Null(message.FailedAt);
        Assert.Single(await ReadReceivedOrdersAsync());
        Assert.Equal([(message.Id, Recorder)], await ReadInboxAsync());

        // Not due yet.
        clock.Advance(TimeSpan.FromSeconds(1));
        await ProcessAsync(api);
        Assert.Equal(1, Assert.Single(await ReadOutboxAsync()).Attempts);

        clock.Advance(TimeSpan.FromSeconds(1));
        await ProcessAsync(api);
        message = Assert.Single(await ReadOutboxAsync());
        Assert.Equal(2, message.Attempts);
        Assert.Equal(clock.GetUtcNow() + TimeSpan.FromSeconds(4), message.NextAttemptAt);
        Assert.Single(await ReadReceivedOrdersAsync());
    }

    [Fact]
    public async Task The_backoff_doubles_up_to_five_minutes_and_the_message_fails_after_ten_attempts()
    {
        var clock = NewClock();
        using var api = WithClock(Api, clock);
        await PlaceOrderAsync(api, InboundFlags.Refuse);

        var waits = new List<double>();
        for (var attempt = 1; attempt < 10; attempt++)
        {
            await ProcessAsync(api);
            var message = Assert.Single(await ReadOutboxAsync());
            Assert.Equal(attempt, message.Attempts);
            var wait = message.NextAttemptAt - clock.GetUtcNow();
            waits.Add(wait.TotalSeconds);
            clock.Advance(wait);
        }

        await ProcessAsync(api);

        Assert.Equal([2, 4, 8, 16, 32, 64, 128, 256, 300], waits);
        var failed = Assert.Single(await ReadOutboxAsync());
        Assert.Equal(10, failed.Attempts);
        Assert.Equal(clock.GetUtcNow(), failed.FailedAt);
        Assert.Null(failed.ProcessedAt);
    }

    // Acceptance criterion: a consumer that keeps failing leaves the message marked failed after the configured
    // attempts, and the failure is logged.
    [Fact]
    public async Task A_consumer_that_keeps_failing_leaves_the_message_failed_after_the_configured_attempts_and_logged()
    {
        var clock = NewClock();
        var logs = new LogCapture();
        using var api = WithClock(Api, clock, builder =>
        {
            builder.UseSetting("Outbox:MaxAttempts", "3");
            builder.ConfigureTestServices(services => services.AddSingleton<ILoggerProvider>(logs));
        });
        await PlaceOrderAsync(api, InboundFlags.Refuse);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await ProcessAsync(api);
            clock.Advance(OutboxBackoffCeiling);
        }

        var message = Assert.Single(await ReadOutboxAsync());
        Assert.Equal(3, message.Attempts);
        Assert.NotNull(message.FailedAt);
        Assert.Null(message.ProcessedAt);
        Assert.Single(await ReadReceivedOrdersAsync());

        var warnings = logs.Entries.Where(entry => entry.Level == LogLevel.Warning).ToList();
        Assert.Equal(3, warnings.Count);
        Assert.All(warnings, entry =>
        {
            Assert.Contains(message.Id.ToString(), entry.Message, StringComparison.Ordinal);
            Assert.Contains(Refuser, entry.Message, StringComparison.Ordinal);
            Assert.Contains(RefuseFlaggedOrderHandler.Refused.Code, entry.Message, StringComparison.Ordinal);
        });
        var gaveUp = Assert.Single(logs.Entries, entry => entry.Level == LogLevel.Error);
        Assert.Contains(message.Id.ToString(), gaveUp.Message, StringComparison.Ordinal);
        Assert.Contains("3 attempts", gaveUp.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_exception_from_a_consumer_is_a_failed_attempt_and_is_logged_with_it()
    {
        var logs = new LogCapture();
        using var api = Api.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<ILoggerProvider>(logs)));
        await PlaceOrderAsync(api, InboundFlags.Throw);

        await ProcessAsync(api);

        var message = Assert.Single(await ReadOutboxAsync());
        Assert.Equal(1, message.Attempts);
        Assert.Null(message.ProcessedAt);
        var warning = Assert.Single(logs.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.IsType<InvalidOperationException>(warning.Exception);
        Assert.Contains(Refuser, warning.Message, StringComparison.Ordinal);
    }

    // The consumer's activity is a child of the request that published the message, so the dashboard shows the
    // delivery inside that request's trace.
    [Fact]
    public async Task Delivery_continues_the_trace_of_the_request_that_published_the_message()
    {
        var activities = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == OutboxTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Enqueue,
        };
        ActivitySource.AddActivityListener(listener);
        using var client = Api.CreateClient();
        var orderId = await CreateOrderAsync("Kari Nordmann");
        using var placed = await client.PostAsync($"/api/sample/orders/{orderId}/place", content: null, TestContext.Current.CancellationToken);
        var traceId = Assert.Single(placed.Headers.GetValues("X-Trace-Id"));

        await ProcessAsync(Api);

        var consumed = activities.Where(activity => activity.TraceId.ToHexString() == traceId).ToList();
        Assert.Equal(2, consumed.Count);
        Assert.All(consumed, activity =>
        {
            Assert.Equal(ActivityKind.Consumer, activity.Kind);
            Assert.Equal("sample.order_placed.v1 consume", activity.DisplayName);
        });
        Assert.Equal([Recorder, Refuser], consumed.Select(activity => (string)activity.GetTagItem("voluntro.outbox.consumer")!).Order());
    }

    [Theory]
    [InlineData("Outbox:MaxAttempts", "0")]
    [InlineData("Outbox:BatchSize", "0")]
    [InlineData("Outbox:PollingInterval", "00:00:00")]
    [InlineData("Outbox:RetentionPeriod", "-1.00:00:00")]
    public void Invalid_outbox_settings_stop_the_api_from_starting(string key, string value)
    {
        using var api = Api.WithWebHostBuilder(builder => builder.UseSetting(key, value));

        var exception = Assert.ThrowsAny<Exception>(() => api.Services);

        Assert.Contains("Invalid Outbox settings", exception.ToString(), StringComparison.Ordinal);
    }

    // Longer than any backoff, so every advance makes the message due again.
    private static readonly TimeSpan OutboxBackoffCeiling = TimeSpan.FromMinutes(5);

    // Starts on a whole microsecond, Postgres' precision, and moves by whole seconds: stored times compare exactly.
    private static FakeTimeProvider NewClock()
    {
        var now = DateTimeOffset.UtcNow;
        return new FakeTimeProvider(now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond)));
    }

    private static WebApplicationFactory<Program> WithClock(
        WebApplicationFactory<Program> api,
        TimeProvider clock,
        Action<IWebHostBuilder>? configure = null) =>
        api.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => services.AddSingleton(clock));
            configure?.Invoke(builder);
        });

    private static Task ProcessAsync(WebApplicationFactory<Program> api) =>
        api.Services.GetRequiredService<OutboxProcessor<SampleDbContext>>().ProcessPendingAsync(TestContext.Current.CancellationToken);

    // A sample order with a line, placed through the API, which publishes sample.order_placed.v1.
    private async Task<SampleOrderId> PlaceOrderAsync(WebApplicationFactory<Program> api, string customerName)
    {
        var orderId = await CreateOrderAsync(customerName);
        using var client = api.CreateClient();
        using var response = await client.PostAsync($"/api/sample/orders/{orderId}/place", content: null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return orderId;
    }

    private async Task<SampleOrderId> CreateOrderAsync(string customerName)
    {
        var order = SampleOrder.Create(customerName, note: null);
        order.AddLine("Hymn book", 1);
        await SaveAsync<SampleDbContext>(order);
        return order.Id;
    }

    private async Task<List<ReceivedOrder>> ReadReceivedOrdersAsync()
    {
        await using var scope = CreateScope();
        return await scope.ServiceProvider.GetRequiredService<InboundDbContext>().ReceivedOrders
            .AsNoTracking()
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private async Task<List<(Guid MessageId, string Consumer)>> ReadInboxAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var command = DataSource.CreateCommand("SELECT message_id, consumer FROM inbound.inbox");
        await using var reader = await command.ExecuteReaderAsync(ct);

        var rows = new List<(Guid, string)>();
        while (await reader.ReadAsync(ct))
        {
            rows.Add((reader.GetGuid(0), reader.GetString(1)));
        }

        return rows;
    }

    private async Task<List<OutboxRow>> ReadOutboxAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var command = DataSource.CreateCommand(
            "SELECT id, type, attempts, next_attempt_at, processed_at, failed_at FROM sample.outbox ORDER BY occurred_at, id");
        await using var reader = await command.ExecuteReaderAsync(ct);

        var rows = new List<OutboxRow>();
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new OutboxRow(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetFieldValue<DateTimeOffset>(3),
                await reader.IsDBNullAsync(4, ct) ? null : reader.GetFieldValue<DateTimeOffset>(4),
                await reader.IsDBNullAsync(5, ct) ? null : reader.GetFieldValue<DateTimeOffset>(5)));
        }

        return rows;
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var command = DataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private NpgsqlDataSource DataSource => Api.Services.GetRequiredService<NpgsqlDataSource>();

    private sealed record OutboxRow(
        Guid Id,
        string Type,
        int Attempts,
        DateTimeOffset NextAttemptAt,
        DateTimeOffset? ProcessedAt,
        DateTimeOffset? FailedAt);

    /// <summary>An event no module consumes.</summary>
    private sealed record NobodyListens : IIntegrationEvent
    {
        public static string EventType => "sample.nobody_listens.v1";
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    /// <summary>Captures what the outbox logs; other categories are ignored.</summary>
    private sealed class LogCapture : ILoggerProvider
    {
        public ConcurrentQueue<LogEntry> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) =>
            categoryName.StartsWith(typeof(OutboxProcessor<>).Namespace!, StringComparison.Ordinal)
                ? new Logger(Entries)
                : Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        public void Dispose()
        {
        }

        private sealed class Logger(ConcurrentQueue<LogEntry> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter) =>
                entries.Enqueue(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }
}
