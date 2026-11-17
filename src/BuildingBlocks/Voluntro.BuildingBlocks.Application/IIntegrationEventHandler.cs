using System.Diagnostics.CodeAnalysis;
using Voluntro.BuildingBlocks.Contracts;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Application;

/// <summary>
/// Consumes an integration event published by a module, usually another one (ADR-0005, ADR-0014). Runs in the
/// consumer's own module and context, in a scope of its own, as the System user. A handler changes tracked entities
/// through the module's narrow context and never saves: its changes are saved together with the inbox record that
/// makes a redelivery skip it.
/// </summary>
/// <remarks>
/// Registered through the consumer module's handler registrar (<c>AddIntegrationEventHandler</c>), never directly.
/// Delivery is at least once, so a handler must be idempotent: the inbox stops most redeliveries, but not all.
/// A failed result and an exception both count as a failed attempt, and the message is retried later.
/// </remarks>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "A handler of integration events (ADR-0005), not a .NET event delegate.")]
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task<Result> HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
