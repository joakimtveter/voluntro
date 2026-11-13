using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.Api.IntegrationTests.Database;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.Api.IntegrationTests.Sample.Persistence;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// The sample aggregate saved to and loaded from a real Postgres, each step in a scope of its own as separate
/// requests would be. Proves the conventions <see cref="PersistenceModelTests"/> checks on the model actually work.
/// </summary>
public sealed class PersistenceTests(PostgresDatabase database) : DatabaseTests(database)
{
    [Fact]
    public async Task A_saved_aggregate_is_read_back_with_its_children()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = SampleOrder.Create("Kari Nordmann", note: null);
        var line = order.AddLine("Hymn book", 3);
        await SaveAsync<SampleDbContext>(order);

        await using var scope = CreateScope();
        var read = await Db(scope).Orders.Include(o => o.Lines).SingleAsync(o => o.Id == order.Id, ct);

        Assert.Equal("Kari Nordmann", read.CustomerName);
        Assert.Equal(SampleOrderStatus.Draft, read.Status);
        Assert.Null(read.Note);
        var readLine = Assert.Single(read.Lines);
        Assert.Equal(line.Id, readLine.Id);
        Assert.Equal(order.Id, readLine.OrderId);
        Assert.Equal("Hymn book", readLine.ProductName);
        Assert.Equal(3, readLine.Quantity);
    }

    [Fact]
    public async Task Saving_assigns_a_version_and_every_update_changes_it()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var scope = CreateScope();
        var db = Db(scope);
        var order = SampleOrder.Create("Kari Nordmann", note: null);

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        var versionWhenCreated = order.Version;

        order.Place();
        await db.SaveChangesAsync(ct);

        Assert.NotEqual(0u, versionWhenCreated);
        Assert.NotEqual(versionWhenCreated, order.Version);
    }

    [Fact]
    public async Task Saving_an_aggregate_that_changed_since_it_was_loaded_throws()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = SampleOrder.Create("Kari Nordmann", note: null);
        await SaveAsync<SampleDbContext>(order);

        await using var mine = CreateScope();
        await using var theirs = CreateScope();
        var myCopy = await Db(mine).Orders.SingleAsync(o => o.Id == order.Id, ct);
        var theirCopy = await Db(theirs).Orders.SingleAsync(o => o.Id == order.Id, ct);

        theirCopy.Place();
        await Db(theirs).SaveChangesAsync(ct);

        myCopy.Place();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => Db(mine).SaveChangesAsync(ct));
    }

    // Its key is already set, so if EF took the key for a generated one it would assume the line exists and
    // UPDATE it, which affects no rows and throws. Keys are never generated, so EF inserts it.
    [Fact]
    public async Task A_child_added_to_a_loaded_aggregate_is_inserted()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = SampleOrder.Create("Kari Nordmann", note: null);
        await SaveAsync<SampleDbContext>(order);

        await using (var scope = CreateScope())
        {
            var db = Db(scope);
            var loaded = await db.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == order.Id, ct);

            loaded.AddLine("Candles", 12);
            await db.SaveChangesAsync(ct);
        }

        await using var check = CreateScope();
        var line = await Db(check).Orders.Where(o => o.Id == order.Id).SelectMany(o => o.Lines).SingleAsync(ct);
        Assert.Equal("Candles", line.ProductName);
    }

    private static SampleDbContext Db(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<SampleDbContext>();
}
