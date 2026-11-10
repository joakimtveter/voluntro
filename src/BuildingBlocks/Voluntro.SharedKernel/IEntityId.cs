namespace Voluntro.SharedKernel;

/// <summary>
/// A strongly-typed identifier wrapping a time-ordered (version 7) <see cref="Guid"/>.
/// Implement it as <c>readonly record struct PersonId(Guid Value) : IEntityId&lt;PersonId&gt;</c>
/// and override <see cref="object.ToString"/> to return the bare Guid.
/// </summary>
/// <typeparam name="TSelf">The implementing ID type.</typeparam>
public interface IEntityId<TSelf>
    where TSelf : struct, IEntityId<TSelf>
{
    Guid Value { get; }

    /// <summary>Creates a new ID from <see cref="Guid.CreateVersion7()"/>.</summary>
    static abstract TSelf New();

    /// <summary>Wraps an existing value. Accepts any Guid, including <see cref="Guid.Empty"/>.</summary>
    static abstract TSelf From(Guid value);
}
