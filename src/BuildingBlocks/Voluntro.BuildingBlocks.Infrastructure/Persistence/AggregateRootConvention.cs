using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// For every entity type deriving from <see cref="AggregateRoot{TId}"/>:
/// <list type="bullet">
///   <item><see cref="AggregateRoot{TId}.Version"/> is the row version on Postgres' <c>xmin</c> system column
///   (ADR-0011). Migrations list it, but Npgsql creates no column for it. A module that configures
///   <c>Version</c> itself owns the whole mapping; the convention then adds nothing to it.</item>
///   <item><see cref="AggregateRoot{TId}.DomainEvents"/> is not mapped; events live in memory until dispatched.</item>
/// </list>
/// </summary>
internal sealed class AggregateRootConvention : IModelFinalizingConvention
{
    private const string XminColumn = "xmin";
    private const string XminType = "xid";

    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        var aggregates = modelBuilder.Metadata.GetEntityTypes()
            .Where(entityType => IsAggregateRoot(entityType.ClrType))
            .ToList();

        foreach (var aggregate in aggregates)
        {
            aggregate.Builder.Ignore(nameof(AggregateRoot<>.DomainEvents));

            // xmin belongs to the row, so only the root of an inheritance hierarchy maps it.
            if (aggregate.BaseType is not null)
            {
                continue;
            }

            // Null when the module ignored Version.
            var version = aggregate.Builder.Property(typeof(uint), nameof(AggregateRoot<>.Version));

            // All or nothing: a module that configured Version itself owns the whole mapping. Each call below
            // yields to explicit configuration only for its own setting, so the rest would still be added,
            // e.g. the xid type on a column the module named: a real column that nothing writes.
            if (version is null || version.Metadata.GetConfigurationSource() != ConfigurationSource.Convention)
            {
                continue;
            }

            version.IsConcurrencyToken(true);
            version.ValueGenerated(ValueGenerated.OnAddOrUpdate);
            version.HasColumnName(XminColumn);
            version.HasColumnType(XminType);
        }
    }

    private static bool IsAggregateRoot(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return true;
            }
        }

        return false;
    }
}
