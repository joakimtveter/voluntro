using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Voluntro.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Primary keys of a strongly-typed ID type are never generated: the domain sets them (<c>PersonId.New()</c>).
/// If EF took such a key for a generated one, a new child added to a loaded aggregate would look like an existing
/// row (its key is already set) and be saved as an <c>UPDATE</c>, which fails with a concurrency exception.
/// </summary>
internal sealed class EntityIdKeyConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        var idKeyProperties = modelBuilder.Metadata.GetEntityTypes()
            .Select(entityType => entityType.FindPrimaryKey())
            .OfType<IConventionKey>()
            .SelectMany(key => key.Properties)
            .Where(property => EntityIds.IsEntityId(property.ClrType))
            .ToList();

        foreach (var property in idKeyProperties)
        {
            property.Builder.ValueGenerated(ValueGenerated.Never);
        }
    }
}
