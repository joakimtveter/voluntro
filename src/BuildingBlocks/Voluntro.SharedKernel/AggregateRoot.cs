namespace Voluntro.SharedKernel;

/// <summary>
/// The entry point to a consistency boundary. Collects the domain events it raises until
/// infrastructure reads and clears them.
/// </summary>
/// <typeparam name="TId">The aggregate's strongly-typed ID.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : struct, IEntityId<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <inheritdoc cref="Entity{TId}(TId)"/>
    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>For EF Core materialization only.</summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// The aggregate's concurrency version (ADR-0011). Only the database assigns it, on every save
    /// (Postgres <c>xmin</c>); it is 0 until the aggregate is first saved. Compare versions for equality
    /// only: the value wraps around, so a larger one is not necessarily newer.
    /// </summary>
    public uint Version { get; private set; }

    /// <summary>
    /// A live, read-only view of the events raised since the last clear.
    /// Copy it before clearing if you need the events afterwards.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
