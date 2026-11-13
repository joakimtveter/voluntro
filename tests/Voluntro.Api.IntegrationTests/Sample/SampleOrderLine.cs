using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>A child entity of <see cref="SampleOrder"/>, with its own ID. Created only through <see cref="SampleOrder.AddLine"/>.</summary>
public sealed class SampleOrderLine : Entity<SampleOrderLineId>
{
    internal SampleOrderLine(SampleOrderLineId id, SampleOrderId orderId, string productName, int quantity)
        : base(id)
    {
        OrderId = orderId;
        ProductName = productName;
        Quantity = quantity;
    }

    public SampleOrderId OrderId { get; private set; }

    public string ProductName { get; private set; }

    public int Quantity { get; private set; }
}
