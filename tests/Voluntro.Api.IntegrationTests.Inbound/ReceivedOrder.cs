using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Inbound;

public readonly record struct ReceivedOrderId(Guid Value) : IEntityId<ReceivedOrderId>
{
    public static ReceivedOrderId New() => new(Guid.CreateVersion7());

    public static ReceivedOrderId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>
/// The inbound module's record of an order placed in the sample module. One per delivered message: a second one for the
/// same order means a redelivery had a second effect.
/// </summary>
public sealed class ReceivedOrder : AggregateRoot<ReceivedOrderId>
{
    // Also the constructor EF materializes through: the parameter names match the mapped properties.
    private ReceivedOrder(ReceivedOrderId id, Guid orderId, string customerName)
        : base(id)
    {
        OrderId = orderId;
        CustomerName = customerName;
    }

    /// <summary>The sample order's ID: another module's, so a plain Guid.</summary>
    public Guid OrderId { get; private set; }

    public string CustomerName { get; private set; }

    public static ReceivedOrder Create(Guid orderId, string customerName) =>
        new(ReceivedOrderId.New(), orderId, customerName);
}
