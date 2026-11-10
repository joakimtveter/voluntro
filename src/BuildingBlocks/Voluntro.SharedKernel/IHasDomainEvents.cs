namespace Voluntro.SharedKernel;

/// <summary>
/// Non-generic access to an aggregate's domain events, so infrastructure (the EF interceptor)
/// can collect and clear them without knowing the aggregate's ID type.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>The events raised since the last clear, in the order they were raised.</summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
