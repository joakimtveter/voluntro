namespace Voluntro.SharedKernel;

/// <summary>
/// An object defined by its identity. Two entities are equal when they have the same
/// runtime type and the same <see cref="Id"/>, whatever their other state.
/// </summary>
/// <typeparam name="TId">The entity's strongly-typed ID.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : struct, IEntityId<TId>
{
    /// <exception cref="ArgumentException"><paramref name="id"/> wraps <see cref="Guid.Empty"/>.</exception>
    protected Entity(TId id)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("An entity ID cannot be empty.", nameof(id));
        }

        Id = id;
    }

    /// <summary>For EF Core materialization only; EF sets <see cref="Id"/> afterwards.</summary>
    protected Entity()
    {
    }

    public TId Id { get; private init; }

    public bool Equals(Entity<TId>? other) =>
        other is not null
        && (ReferenceEquals(this, other) || (GetType() == other.GetType() && Id.Equals(other.Id)));

    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
