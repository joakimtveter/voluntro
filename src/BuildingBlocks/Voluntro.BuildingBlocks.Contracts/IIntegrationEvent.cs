namespace Voluntro.BuildingBlocks.Contracts;

/// <summary>
/// Something that happened in one module that other modules may react to (ADR-0005). Declared as a record in the
/// publishing module's Contracts, published through the module's outbox, and delivered at least once to every consumer.
/// Carries no ID or timestamp: those are on the outbox message.
/// </summary>
/// <remarks>
/// Because of the static abstract member, this interface can't be used as a type argument (CS8920): APIs that take an
/// integration event are generic, e.g. <c>Add&lt;TEvent&gt;(TEvent integrationEvent) where TEvent : IIntegrationEvent</c>.
/// </remarks>
public interface IIntegrationEvent
{
    /// <summary>
    /// The event's stable, versioned name, <c>{moduleKey}.{snake_case}.v{N}</c>, e.g.
    /// <c>identity.user_registered.v1</c>. It is stored with every message and is what consumers are matched by, so it
    /// never changes: a change to the payload is a new event with the next version. An architecture test checks the
    /// format, the module key and that names are unique.
    /// </summary>
    static abstract string EventType { get; }
}
