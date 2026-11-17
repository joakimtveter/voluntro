using Microsoft.EntityFrameworkCore;
using Voluntro.Api.IntegrationTests.Sample.Contracts;
using Voluntro.BuildingBlocks.Application;
using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>Raised by <see cref="SampleOrder.Place"/>.</summary>
public sealed record SampleOrderPlaced(SampleOrderId OrderId, SampleCustomerId? CustomerId, string CustomerName) : IDomainEvent;

/// <summary>
/// Tells other modules that an order was placed, through the module's outbox (ADR-0005): the integration event is
/// saved with the order, or not at all.
/// </summary>
public sealed class PublishOrderPlacedHandler(ISampleDbContext db) : IDomainEventHandler<SampleOrderPlaced>
{
    public Task<Result> HandleAsync(SampleOrderPlaced domainEvent, CancellationToken cancellationToken)
    {
        db.Add(new OrderPlaced(domainEvent.OrderId.Value, domainEvent.CustomerName));
        return Task.FromResult(Result.Success());
    }
}

/// <summary>
/// Counts a placed order on its customer, in the same save as the order (ADR-0013). An order without a customer has
/// nothing to count. An order whose customer doesn't exist fails, which fails the whole command: the order isn't
/// placed either.
/// </summary>
public sealed class CountPlacedOrderHandler(ISampleDbContext db) : IDomainEventHandler<SampleOrderPlaced>
{
    public async Task<Result> HandleAsync(SampleOrderPlaced domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.CustomerId is not { } customerId)
        {
            return Result.Success();
        }

        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == customerId, cancellationToken);
        if (customer is null)
        {
            return SampleErrors.CustomerNotFound with { Detail = $"Sample customer {customerId} does not exist." };
        }

        customer.CountPlacedOrder();
        return Result.Success();
    }
}
