using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.Api.IntegrationTests.Database;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.Api.IntegrationTests.Sample.Persistence;
using Voluntro.BuildingBlocks.Application;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// The audit interceptor against a real Postgres: who and when on every save, and a change to a child entity stamps
/// its root, so the root's row is written and its version checked (ADR-0011).
/// </summary>
public sealed class AuditTests(PostgresDatabase database) : DatabaseTests(database)
{
    // Postgres keeps microseconds; a .NET timestamp has ten ticks to the microsecond.
    private static readonly TimeSpan Precision = TimeSpan.FromMicroseconds(1);

    [Fact]
    public async Task A_created_aggregate_is_stamped_by_the_system_user_until_identity_exists()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = Api.CreateClient();
        var before = DateTimeOffset.UtcNow;

        using var response = await client.PostAsJsonAsync("/api/sample/orders", new { customerName = "Kari Nordmann" }, ct);
        var after = DateTimeOffset.UtcNow;

        var order = await ReadAsync(await CreatedIdAsync(response));
        Assert.Equal(SystemUser.Id, order.CreatedBy);
        Assert.InRange(order.CreatedAt, before - Precision, after + Precision);
        Assert.Equal(TimeSpan.Zero, order.CreatedAt.Offset);
        Assert.Equal(order.CreatedAt, order.ModifiedAt);
        Assert.Equal(order.CreatedBy, order.ModifiedBy);
    }

    [Fact]
    public async Task The_stamps_name_the_current_user()
    {
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.CreateVersion7();
        using var signedIn = Api.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddScoped<ICurrentUser>(_ => new FixedUser(userId))));
        using var client = signedIn.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/sample/orders", new { customerName = "Kari Nordmann" }, ct);

        var order = await ReadAsync(await CreatedIdAsync(response));
        Assert.Equal(userId, order.CreatedBy);
        Assert.Equal(userId, order.ModifiedBy);
    }

    [Fact]
    public async Task A_change_to_the_root_stamps_it_modified_and_keeps_the_creation_stamp()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = SampleOrder.Create("Kari Nordmann", note: null);
        order.AddLine("Hymn book", 3);
        await SaveAsync<SampleDbContext>(order);
        var created = await ReadAsync(order.Id);
        using var client = Api.CreateClient();

        using var response = await client.PostAsync($"/api/sample/orders/{order.Id}/place", content: null, ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var placed = await ReadAsync(order.Id);
        Assert.Equal(created.CreatedAt, placed.CreatedAt);
        Assert.True(placed.ModifiedAt > created.ModifiedAt, "Placing the order stamps it modified.");
    }

    [Fact]
    public async Task A_change_to_a_child_alone_stamps_the_root_and_changes_its_version()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = SampleOrder.Create("Kari Nordmann", note: null);
        await SaveAsync<SampleDbContext>(order);
        var created = await ReadAsync(order.Id);

        await using (var scope = CreateScope())
        {
            var db = Db(scope);
            var loaded = await db.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == order.Id, ct);

            loaded.AddLine("Candles", 12);
            await db.SaveChangesAsync(ct);
        }

        var changed = await ReadAsync(order.Id);
        Assert.True(changed.ModifiedAt > created.ModifiedAt, "Adding a line stamps the order modified.");
        Assert.NotEqual(created.Version, changed.Version);
    }

    [Fact]
    public async Task Concurrent_changes_to_children_alone_are_a_concurrency_conflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = SampleOrder.Create("Kari Nordmann", note: null);
        await SaveAsync<SampleDbContext>(order);

        await using var mine = CreateScope();
        await using var theirs = CreateScope();
        var myCopy = await Db(mine).Orders.Include(o => o.Lines).SingleAsync(o => o.Id == order.Id, ct);
        var theirCopy = await Db(theirs).Orders.Include(o => o.Lines).SingleAsync(o => o.Id == order.Id, ct);

        theirCopy.AddLine("Candles", 12);
        await Db(theirs).SaveChangesAsync(ct);

        myCopy.AddLine("Hymn book", 3);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => Db(mine).SaveChangesAsync(ct));
    }

    [Fact]
    public async Task Changing_a_child_without_loading_its_aggregate_throws()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = SampleOrder.Create("Kari Nordmann", note: null);
        order.AddLine("Hymn book", 3);
        await SaveAsync<SampleDbContext>(order);

        await using var scope = CreateScope();
        var db = Db(scope);
        var line = await db.Set<SampleOrderLine>().SingleAsync(ct);

        db.Remove(line);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(ct));
        Assert.Contains("isn't tracked", exception.Message, StringComparison.Ordinal);
    }

    private static SampleDbContext Db(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<SampleDbContext>();

    private static async Task<SampleOrderId> CreatedIdAsync(HttpResponseMessage response)
    {
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return SampleOrderId.From(body.RootElement.GetProperty("id").GetGuid());
    }

    private async Task<SampleOrder> ReadAsync(SampleOrderId id)
    {
        await using var scope = CreateScope();
        return await Db(scope).Orders.AsNoTracking().SingleAsync(o => o.Id == id, TestContext.Current.CancellationToken);
    }

    private sealed class FixedUser(Guid userId) : ICurrentUser
    {
        public Guid UserId => userId;
    }
}
