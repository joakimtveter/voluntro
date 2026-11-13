using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.Api.IntegrationTests.Database;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.Api.IntegrationTests.Sample.Persistence;
using Voluntro.BuildingBlocks.Application;
using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// The transaction decorator against a real Postgres: a command's changes are saved only when it succeeds (ADR-0012).
/// </summary>
public sealed class TransactionTests(PostgresDatabase database) : DatabaseTests(database)
{
    [Fact]
    public async Task A_successful_command_is_saved()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = SampleOrder.Create("Kari Nordmann", note: null);
        order.AddLine("Hymn book", 3);
        await SaveAsync<SampleDbContext>(order);
        using var client = Api.CreateClient();

        using var response = await client.PostAsync($"/api/sample/orders/{order.Id}/place", content: null, ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(SampleOrderStatus.Placed, await ReadStatusAsync(order.Id));
    }

    // The handler places the order before it finds there are no lines, so the change tracker holds a Placed order
    // when the failure is returned. The decorator must persist none of it.
    [Fact]
    public async Task A_failed_command_persists_nothing_even_after_changing_tracked_entities()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = SampleOrder.Create("Kari Nordmann", note: null);
        await SaveAsync<SampleDbContext>(order);
        using var client = Api.CreateClient();

        using var response = await client.PostAsync($"/api/sample/orders/{order.Id}/place", content: null, ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        Assert.Equal("sample.order.empty", body.RootElement.GetProperty("code").GetString());
        Assert.Equal(SampleOrderStatus.Draft, await ReadStatusAsync(order.Id));
    }

    [Fact]
    public async Task A_failed_command_leaves_nothing_for_a_later_save_in_the_same_scope()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = SampleOrder.Create("Kari Nordmann", note: null);
        await SaveAsync<SampleDbContext>(order);

        await using (var scope = CreateScope())
        {
            var place = scope.ServiceProvider.GetRequiredService<ICommandHandler<PlaceSampleOrder>>();
            var result = await place.HandleAsync(new PlaceSampleOrder(order.Id), ct);
            Assert.Equal(SampleErrors.OrderEmpty, result.Error);

            await scope.ServiceProvider.GetRequiredService<SampleDbContext>().SaveChangesAsync(ct);
        }

        Assert.Equal(SampleOrderStatus.Draft, await ReadStatusAsync(order.Id));
    }

    [Fact]
    public async Task Saving_an_aggregate_that_changed_since_it_was_loaded_is_a_concurrency_conflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = SampleOrder.Create("Kari Nordmann", note: null);
        await SaveAsync<SampleDbContext>(order);

        await using var mine = CreateScope();
        await using var theirs = CreateScope();
        var myDb = mine.ServiceProvider.GetRequiredService<SampleDbContext>();
        var theirDb = theirs.ServiceProvider.GetRequiredService<SampleDbContext>();
        var myCopy = await myDb.Orders.SingleAsync(o => o.Id == order.Id, ct);
        var theirCopy = await theirDb.Orders.SingleAsync(o => o.Id == order.Id, ct);

        theirCopy.Place();
        await theirDb.SaveChangesAsync(ct);

        myCopy.Place();
        var result = await ((IUnitOfWork)myDb).SaveChangesAsync(ct);

        Assert.Equal(CommonErrors.ConcurrencyConflict, result.Error);
    }

    private async Task<SampleOrderStatus> ReadStatusAsync(SampleOrderId id)
    {
        await using var scope = CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SampleDbContext>().Orders
            .Where(o => o.Id == id)
            .Select(o => o.Status)
            .SingleAsync(TestContext.Current.CancellationToken);
    }
}
