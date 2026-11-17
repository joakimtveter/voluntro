using System.Diagnostics.CodeAnalysis;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Application;

/// <summary>
/// Reacts to a domain event inside the module that raised it, in the same save as the change that raised it
/// (ADR-0013). A handler changes tracked entities through the module's narrow context, may query, and never saves.
/// A failed result fails the whole command: later handlers don't run and nothing is saved. Reacting in another module
/// goes through an integration event instead.
/// </summary>
/// <remarks>
/// Registered through the module's handler registrar (<c>AddDomainEventHandler</c>), never directly. Several handlers
/// for one event run in registration order; handlers must not depend on the order of different events.
/// </remarks>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "A handler of domain events (ADR-0013), not a .NET event delegate.")]
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task<Result> HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
