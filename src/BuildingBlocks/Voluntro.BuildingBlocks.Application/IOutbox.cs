using Voluntro.BuildingBlocks.Contracts;

namespace Voluntro.BuildingBlocks.Application;

/// <summary>
/// Publishes integration events from a module (ADR-0005, ADR-0014). Every module's DbContext implements it, and the
/// module's narrow context interface inherits it (<c>ISampleDbContext : IOutbox</c>), so command handlers and domain
/// event handlers publish through the context they already have, and the event always lands in their own module's
/// outbox.
/// </summary>
public interface IOutbox
{
    /// <summary>
    /// Adds the event to the module's outbox as part of the current change: it is saved with the command, or discarded
    /// with it. Nothing is delivered before the save commits; the outbox processor delivers it afterwards.
    /// </summary>
    void Add<TEvent>(TEvent integrationEvent)
        where TEvent : IIntegrationEvent;
}
