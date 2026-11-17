using Voluntro.Api.IntegrationTests.Sample.Contracts;
using Voluntro.BuildingBlocks.Application;
using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Inbound;

/// <summary>Records every placed order it is given. Its inbox makes a redelivery skip it, so one order is one record.</summary>
public sealed class RecordReceivedOrderHandler(IInboundDbContext db) : IIntegrationEventHandler<OrderPlaced>
{
    public Task<Result> HandleAsync(OrderPlaced integrationEvent, CancellationToken cancellationToken)
    {
        db.ReceivedOrders.Add(ReceivedOrder.Create(integrationEvent.OrderId, integrationEvent.CustomerName));
        return Task.FromResult(Result.Success());
    }
}

/// <summary>
/// Fails on demand, so tests can prove retries, backoff and giving up: an order placed for
/// <see cref="InboundFlags.Refuse"/> fails with an error, one for <see cref="InboundFlags.Throw"/> throws. Every other
/// order passes untouched.
/// </summary>
public sealed class RefuseFlaggedOrderHandler : IIntegrationEventHandler<OrderPlaced>
{
    public static readonly Error Refused = Error.Conflict("inbound.order.refused");

    public Task<Result> HandleAsync(OrderPlaced integrationEvent, CancellationToken cancellationToken) =>
        integrationEvent.CustomerName switch
        {
            InboundFlags.Refuse => Task.FromResult(Result.Failure(Refused)),
            InboundFlags.Throw => throw new InvalidOperationException("Told to throw."),
            _ => Task.FromResult(Result.Success()),
        };
}

/// <summary>Customer names that make <see cref="RefuseFlaggedOrderHandler"/> fail.</summary>
public static class InboundFlags
{
    public const string Refuse = "Refuse Me";

    public const string Throw = "Throw Me";
}
