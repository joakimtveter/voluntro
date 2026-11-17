using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Voluntro.Api.IntegrationTests.Database;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.Api.IntegrationTests.Sample.Persistence;
using Voluntro.BuildingBlocks.Application;
using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// Updates carry the version the client read; a stale one is a 409 <c>common.concurrency_conflict</c>, whether the
/// handler's version check or the <c>xmin</c> check on save catches it (ADR-0011).
/// </summary>
public sealed class ConcurrencyTests(PostgresDatabase database) : DatabaseTests(database)
{
    [Fact]
    public async Task An_update_with_the_current_version_is_saved_and_changes_the_version()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = await SavedOrderAsync();
        using var client = Api.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/sample/orders/{order.Id}",
            new { customerName = "Ola Nordmann", note = "Ring first", version = order.Version },
            ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await ReadAsync(order.Id);
        Assert.Equal("Ola Nordmann", updated.CustomerName);
        Assert.Equal("Ring first", updated.Note);
        Assert.NotEqual(order.Version, updated.Version);
    }

    [Fact]
    public async Task An_update_with_a_stale_version_is_a_conflict_and_changes_nothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = await SavedOrderAsync();
        using var client = Api.CreateClient();
        var url = $"/api/sample/orders/{order.Id}";
        using var first = await client.PutAsJsonAsync(url, new { customerName = "Ola Nordmann", version = order.Version }, ct);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        // The same version again: someone else's update has moved the order on since it was read.
        using var stale = await client.PutAsJsonAsync(url, new { customerName = "Per Hansen", version = order.Version }, ct);

        await AssertConflictAsync(stale);
        Assert.Equal("Ola Nordmann", (await ReadAsync(order.Id)).CustomerName);
    }

    // A missing version binds as 0; without the validator's check it would be a 409 that no reload can fix.
    [Fact]
    public async Task An_update_without_a_version_is_a_validation_error()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = await SavedOrderAsync();
        using var client = Api.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/sample/orders/{order.Id}", new { customerName = "Ola Nordmann" }, ct);

        await AssertVersionRequiredAsync(response);
    }

    [Fact]
    public async Task A_line_added_without_a_version_is_a_validation_error()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = await SavedOrderAsync();
        using var client = Api.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/sample/orders/{order.Id}/lines", new { productName = "Candles", quantity = 12 }, ct);

        await AssertVersionRequiredAsync(response);
        Assert.Empty((await ReadAsync(order.Id)).Lines);
    }

    [Fact]
    public async Task An_update_of_an_unknown_order_is_not_found()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = Api.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/sample/orders/{Guid.CreateVersion7()}",
            new { customerName = "Ola Nordmann", version = 1 },
            ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_line_added_with_the_current_version_answers_201_pointing_at_its_order()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = await SavedOrderAsync();
        using var client = Api.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/sample/orders/{order.Id}/lines",
            new { productName = "Candles", quantity = 12, version = order.Version },
            ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/sample/orders/{order.Id}", response.Headers.Location?.AbsolutePath);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var lineId = SampleOrderLineId.From(body.RootElement.GetProperty("id").GetGuid());

        var updated = await ReadAsync(order.Id);
        var line = Assert.Single(updated.Lines);
        Assert.Equal(lineId, line.Id);
        Assert.NotEqual(order.Version, updated.Version);
    }

    [Fact]
    public async Task A_line_added_with_a_stale_version_is_a_conflict_and_adds_nothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = await SavedOrderAsync();
        using var client = Api.CreateClient();
        var url = $"/api/sample/orders/{order.Id}/lines";
        using var first = await client.PostAsJsonAsync(url, new { productName = "Candles", quantity = 12, version = order.Version }, ct);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        using var stale = await client.PostAsJsonAsync(url, new { productName = "Hymn book", quantity = 3, version = order.Version }, ct);

        await AssertConflictAsync(stale);
        Assert.Single((await ReadAsync(order.Id)).Lines);
    }

    [Fact]
    public async Task An_invalid_line_is_rejected_before_the_version_is_checked()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = await SavedOrderAsync();
        using var client = Api.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/sample/orders/{order.Id}/lines",
            new { productName = "", quantity = 0, version = order.Version + 1 },
            ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var errors = body.RootElement.GetProperty("errors");
        Assert.Equal("required", errors.GetProperty("productName")[0].GetString());
        Assert.Equal("out_of_range", errors.GetProperty("quantity")[0].GetString());
    }

    // The handler's check passes, because the order it loaded was already tracked at the version the client sent.
    // Someone else saves before this command does, so only the xmin check on save can catch it, and the transaction
    // decorator answers with the same error the handler's check would have.
    [Fact]
    public async Task An_edit_between_the_version_check_and_the_save_is_the_same_conflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = await SavedOrderAsync();

        await using var mine = CreateScope();
        await Db(mine).Orders.SingleAsync(o => o.Id == order.Id, ct);

        await using (var theirs = CreateScope())
        {
            var theirCopy = await Db(theirs).Orders.SingleAsync(o => o.Id == order.Id, ct);
            theirCopy.Update("Per Hansen", note: null);
            await Db(theirs).SaveChangesAsync(ct);
        }

        var update = mine.ServiceProvider.GetRequiredService<ICommandHandler<UpdateSampleOrder>>();
        var result = await update.HandleAsync(new UpdateSampleOrder(order.Id, "Ola Nordmann", null, order.Version), ct);

        Assert.Equal(CommonErrors.ConcurrencyConflict, result.Error);
        Assert.Equal("Per Hansen", (await ReadAsync(order.Id)).CustomerName);
    }

    // A bug that updates a missing row fails the same way as a real conflict; the rows in the log tell them apart.
    [Fact]
    public async Task A_conflict_on_save_is_logged_with_the_rows_it_hit()
    {
        var ct = TestContext.Current.CancellationToken;
        var logs = new LogCapture();
        using var api = Api.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<ILoggerProvider>(logs)));
        var order = await SavedOrderAsync();

        await using var mine = api.Services.CreateAsyncScope();
        await Db(mine).Orders.SingleAsync(o => o.Id == order.Id, ct);

        await using (var theirs = api.Services.CreateAsyncScope())
        {
            var theirCopy = await Db(theirs).Orders.SingleAsync(o => o.Id == order.Id, ct);
            theirCopy.Update("Per Hansen", note: null);
            await Db(theirs).SaveChangesAsync(ct);
        }

        var update = mine.ServiceProvider.GetRequiredService<ICommandHandler<UpdateSampleOrder>>();
        var result = await update.HandleAsync(new UpdateSampleOrder(order.Id, "Ola Nordmann", null, order.Version), ct);

        Assert.Equal(CommonErrors.ConcurrencyConflict, result.Error);
        var conflict = Assert.Single(logs.Messages, message => message.Contains("concurrency conflict", StringComparison.Ordinal));
        Assert.Contains($"SampleOrder {order.Id}", conflict, StringComparison.Ordinal);
        Assert.DoesNotContain("Ola Nordmann", conflict, StringComparison.Ordinal);
    }

    private static SampleDbContext Db(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<SampleDbContext>();

    private sealed class LogCapture : ILoggerProvider
    {
        public ConcurrentQueue<string> Messages { get; } = new();

        public ILogger CreateLogger(string categoryName) => new Logger(Messages);

        public void Dispose()
        {
        }

        private sealed class Logger(ConcurrentQueue<string> messages) : ILogger
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
                messages.Enqueue(formatter(state, exception));
        }
    }

    private static async Task AssertConflictAsync(HttpResponseMessage response)
    {
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        Assert.Equal("common.concurrency_conflict", body.RootElement.GetProperty("code").GetString());
    }

    private static async Task AssertVersionRequiredAsync(HttpResponseMessage response)
    {
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var errors = body.RootElement.GetProperty("errors");
        Assert.Equal(["version"], errors.EnumerateObject().Select(field => field.Name));
        Assert.Equal("required", errors.GetProperty("version")[0].GetString());
    }

    /// <summary>A saved order, read back so its version is the one the database assigned.</summary>
    private async Task<SampleOrder> SavedOrderAsync()
    {
        var order = SampleOrder.Create("Kari Nordmann", note: null);
        await SaveAsync<SampleDbContext>(order);
        return await ReadAsync(order.Id);
    }

    private async Task<SampleOrder> ReadAsync(SampleOrderId id)
    {
        await using var scope = CreateScope();
        return await Db(scope).Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .SingleAsync(o => o.Id == id, TestContext.Current.CancellationToken);
    }
}
