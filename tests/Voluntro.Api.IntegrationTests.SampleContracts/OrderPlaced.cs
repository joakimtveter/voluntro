using Voluntro.BuildingBlocks.Contracts;

namespace Voluntro.Api.IntegrationTests.Sample.Contracts;

/// <summary>
/// The sample module's integration event: an order was placed. Plays the part of a real module's Contracts, so it
/// uses plain <see cref="Guid"/>s, not the module's ID types.
/// </summary>
public sealed record OrderPlaced(Guid OrderId, string CustomerName) : IIntegrationEvent
{
    public static string EventType => "sample.order_placed.v1";
}
