using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.Api.IntegrationTests.Database;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.Api.IntegrationTests.Sample.Persistence;
using Voluntro.BuildingBlocks.Application;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// Domain events against a real Postgres: a handler's changes commit or roll back together with the change that raised
/// the event (ADR-0013). Placing a sample order raises <see cref="SampleOrderPlaced"/>, whose handler counts it on the
/// order's customer.
/// </summary>
public sealed class DomainEventTests(PostgresDatabase database) : DatabaseTests(database)
{
    [Fact]
    public async Task A_handlers_change_is_saved_with_the_change_that_raised_the_event()
    {
        var ct = TestContext.Current.CancellationToken;
        var customer = SampleCustomer.Create("Kari Nordmann");
        var order = SampleOrder.Create("Kari Nordmann", note: null, customer.Id);
        order.AddLine("Hymn book", 3);
        await SaveAsync<SampleDbContext>(customer, order);
        var before = await ReadCustomerAsync(customer.Id);
        using var client = Api.CreateClient();

        using var response = await client.PostAsync($"/api/sample/orders/{order.Id}/place", content: null, ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(SampleOrderStatus.Placed, (await ReadOrderAsync(order.Id)).Status);
        var after = await ReadCustomerAsync(customer.Id);
        Assert.Equal(1, after.PlacedOrders);

        // Saved by the same save as the order, so the audit interceptor stamped it and its row was version-checked.
        Assert.NotEqual(before.Version, after.Version);
        Assert.True(after.ModifiedAt > before.ModifiedAt);
    }

    [Fact]
    public async Task A_failing_handler_rolls_back_the_change_that_raised_the_event()
    {
        var ct = TestContext.Current.CancellationToken;
        var order = SampleOrder.Create("Kari Nordmann", note: null, SampleCustomerId.New());
        order.AddLine("Hymn book", 3);
        await SaveAsync<SampleDbContext>(order);
        using var client = Api.CreateClient();

        using var response = await client.PostAsync($"/api/sample/orders/{order.Id}/place", content: null, ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        Assert.Equal("sample.customer.not_found", body.RootElement.GetProperty("code").GetString());
        Assert.Equal(SampleOrderStatus.Draft, (await ReadOrderAsync(order.Id)).Status);
    }

    // One event's handler counts an order on its customer, another's fails: the unit of work returns the failure
    // without saving, so the count doesn't reach the database either, whichever event is handled first.
    [Fact]
    public async Task A_failing_handler_rolls_back_changes_other_handlers_already_made()
    {
        var ct = TestContext.Current.CancellationToken;
        var customer = SampleCustomer.Create("Kari Nordmann");
        var counted = SampleOrder.Create("Kari Nordmann", note: null, customer.Id);
        var orphan = SampleOrder.Create("Ola Nordmann", note: null, SampleCustomerId.New());
        await SaveAsync<SampleDbContext>(customer, counted, orphan);

        await using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
            (await db.Orders.SingleAsync(o => o.Id == counted.Id, ct)).Place();
            (await db.Orders.SingleAsync(o => o.Id == orphan.Id, ct)).Place();

            var unitOfWork = scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(typeof(SampleDbContext));
            var result = await unitOfWork.SaveChangesAsync(ct);

            Assert.Equal(SampleErrors.CustomerNotFound.Code, result.Error?.Code);
        }

        Assert.Equal(0, (await ReadCustomerAsync(customer.Id)).PlacedOrders);
        Assert.Equal(SampleOrderStatus.Draft, (await ReadOrderAsync(counted.Id)).Status);
    }

    // Only the unit of work dispatches (ADR-0013); a test helper's direct save, like this one, doesn't.
    [Fact]
    public async Task A_direct_save_dispatches_nothing()
    {
        var customer = SampleCustomer.Create("Kari Nordmann");
        var order = SampleOrder.Create("Kari Nordmann", note: null, customer.Id);
        order.Place();

        await SaveAsync<SampleDbContext>(customer, order);

        Assert.Equal(0, (await ReadCustomerAsync(customer.Id)).PlacedOrders);
    }

    private async Task<SampleCustomer> ReadCustomerAsync(SampleCustomerId id)
    {
        await using var scope = CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SampleDbContext>().Customers
            .AsNoTracking()
            .SingleAsync(c => c.Id == id, TestContext.Current.CancellationToken);
    }

    private async Task<SampleOrder> ReadOrderAsync(SampleOrderId id)
    {
        await using var scope = CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SampleDbContext>().Orders
            .AsNoTracking()
            .SingleAsync(o => o.Id == id, TestContext.Current.CancellationToken);
    }
}
