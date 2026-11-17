using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>
/// A second aggregate in the sample module, changed only by a domain event handler: placing an order counts it here.
/// It exists to prove that a handler's changes are saved, stamped and version-checked together with the change that
/// raised the event, or not at all (ADR-0013).
/// </summary>
public sealed class SampleCustomer : AggregateRoot<SampleCustomerId>
{
    // Also the constructor EF materializes through: the parameter names match the mapped properties.
    private SampleCustomer(SampleCustomerId id, string name)
        : base(id)
    {
        Name = name;
    }

    public string Name { get; private set; }

    public int PlacedOrders { get; private set; }

    public static SampleCustomer Create(string name) => new(SampleCustomerId.New(), name);

    public void CountPlacedOrder() => PlacedOrders++;
}
