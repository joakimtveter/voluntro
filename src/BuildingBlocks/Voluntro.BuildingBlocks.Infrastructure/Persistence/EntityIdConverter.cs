using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Stores a strongly-typed ID as its <see cref="Guid"/> (<c>uuid</c>). <see cref="ModuleDbContext"/> registers one
/// for every <see cref="IEntityId{TSelf}"/> type, so keys and foreign keys need no per-entity configuration.
/// </summary>
/// <typeparam name="TId">The ID type.</typeparam>
public sealed class EntityIdConverter<TId>() : ValueConverter<TId, Guid>(id => id.Value, value => FromGuid(value))
    where TId : struct, IEntityId<TId>
{
    // An expression tree can't call a static abstract interface member (CS8927), so the conversion above
    // reaches TId.From through this ordinary method.
    private static TId FromGuid(Guid value) => TId.From(value);
}
