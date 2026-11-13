using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Persistence;

internal static class EntityIds
{
    /// <summary>Whether <paramref name="type"/> is a strongly-typed ID, i.e. implements <see cref="IEntityId{TSelf}"/> of itself.</summary>
    public static bool IsEntityId(Type type) =>
        type.IsValueType
        && type.GetInterfaces().Any(contract =>
            contract.IsGenericType
            && contract.GetGenericTypeDefinition() == typeof(IEntityId<>)
            && contract.GetGenericArguments()[0] == type);
}
