using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Persistence;

internal static class AggregateRoots
{
    /// <summary>Whether <paramref name="type"/> derives from <see cref="AggregateRoot{TId}"/>, at any depth.</summary>
    public static bool IsAggregateRoot(Type type) => DerivesFrom(type, typeof(AggregateRoot<>));

    /// <summary>Whether <paramref name="type"/> derives from <see cref="Entity{TId}"/>, at any depth.</summary>
    public static bool IsEntity(Type type) => DerivesFrom(type, typeof(Entity<>));

    private static bool DerivesFrom(Type type, Type genericBase)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == genericBase)
            {
                return true;
            }
        }

        return false;
    }
}
