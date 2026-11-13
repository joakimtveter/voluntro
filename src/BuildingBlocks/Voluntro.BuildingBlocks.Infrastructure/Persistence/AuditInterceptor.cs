using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Voluntro.BuildingBlocks.Application;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Stamps the audit properties of <see cref="AggregateRoot{TId}"/> as part of every save, inside its transaction:
/// <list type="bullet">
///   <item>An added root gets <c>Created*</c> and <c>Modified*</c>, both the same.</item>
///   <item>A modified root gets <c>Modified*</c>.</item>
///   <item>An added, modified or deleted child entity stamps its root's <c>Modified*</c>. That writes the root's row,
///   with its <c>xmin</c> check, so concurrent edits of children are detected like edits of the root (ADR-0011).</item>
/// </list>
/// Added to every module context by <c>AddModuleDbContext</c>.
/// </summary>
/// <remarks>
/// A child's root is found through EF's metadata: the foreign key whose principal has a navigation to the child
/// (<c>HasMany(order =&gt; order.Lines)</c>), followed up to an aggregate root. A reference by ID to another aggregate
/// has no such navigation and is never followed. The root must be tracked: changing a child without loading its
/// aggregate is a bug, and saving throws.
/// </remarks>
internal sealed class AuditInterceptor(ICurrentUser currentUser, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Stamp(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Stamp(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        // SavingChanges runs before EF detects changes itself; a line added to a loaded order through its
        // private list is only Added once changes are detected.
        context.ChangeTracker.DetectChanges();

        var now = timeProvider.GetUtcNow();
        var userId = currentUser.UserId;
        var entries = context.ChangeTracker.Entries().ToList();
        var rootsToStamp = new HashSet<EntityEntry>(ReferenceEqualityComparer.Instance);

        foreach (var entry in entries)
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var type = entry.Metadata.ClrType;
            if (AggregateRoots.IsAggregateRoot(type))
            {
                if (entry.State == EntityState.Added)
                {
                    StampCreated(entry, now, userId);
                }
                else if (entry.State == EntityState.Modified)
                {
                    rootsToStamp.Add(entry);
                }
            }
            else if (AggregateRoots.IsEntity(type))
            {
                var root = FindRoot(entry, entries);

                // A new root is stamped as created; a deleted one takes its children with it.
                if (root.State is EntityState.Unchanged or EntityState.Modified)
                {
                    rootsToStamp.Add(root);
                }
            }
        }

        foreach (var root in rootsToStamp)
        {
            // Setting a property through the entry marks it, and so the root, Modified.
            root.Property(nameof(AggregateRoot<>.ModifiedAt)).CurrentValue = now;
            root.Property(nameof(AggregateRoot<>.ModifiedBy)).CurrentValue = userId;
        }
    }

    private static void StampCreated(EntityEntry entry, DateTimeOffset now, Guid userId)
    {
        entry.Property(nameof(AggregateRoot<>.CreatedAt)).CurrentValue = now;
        entry.Property(nameof(AggregateRoot<>.CreatedBy)).CurrentValue = userId;
        entry.Property(nameof(AggregateRoot<>.ModifiedAt)).CurrentValue = now;
        entry.Property(nameof(AggregateRoot<>.ModifiedBy)).CurrentValue = userId;
    }

    /// <summary>Follows the owning foreign keys up from a child entity to its tracked aggregate root.</summary>
    /// <exception cref="InvalidOperationException">
    /// The entity has no owner in the model, or its owner isn't tracked.
    /// </exception>
    private static EntityEntry FindRoot(EntityEntry child, IReadOnlyList<EntityEntry> entries)
    {
        var current = child;

        while (!AggregateRoots.IsAggregateRoot(current.Metadata.ClrType))
        {
            var ownership = current.Metadata.GetForeignKeys().SingleOrDefault(fk => fk.PrincipalToDependent is not null)
                ?? throw new InvalidOperationException(
                    $"{current.Metadata.ClrType.Name} is an entity but no aggregate owns it: none of its foreign keys " +
                    "has a navigation from the principal (HasMany(root => root.Children)). Configure it as a child " +
                    "of its aggregate root, or make it an aggregate root itself.");

            current = FindPrincipal(current, ownership, entries)
                ?? throw new InvalidOperationException(
                    $"A {child.Metadata.ClrType.Name} changed, but its {ownership.PrincipalEntityType.ClrType.Name} " +
                    "isn't tracked. Load the aggregate and change the child through it, so the root's version is " +
                    "checked and its audit stamp updated.");
        }

        return current;
    }

    private static EntityEntry? FindPrincipal(EntityEntry dependent, IForeignKey foreignKey, IReadOnlyList<EntityEntry> entries)
    {
        // A deleted row's foreign key may already be cleared by a cascade; its original value still names the owner.
        var foreignKeyValues = foreignKey.Properties
            .Select(property => dependent.State == EntityState.Deleted
                ? dependent.Property(property.Name).OriginalValue
                : dependent.Property(property.Name).CurrentValue)
            .ToList();

        return entries.FirstOrDefault(candidate =>
            foreignKey.PrincipalEntityType.IsAssignableFrom(candidate.Metadata)
            && foreignKey.PrincipalKey.Properties
                .Select(property => candidate.Property(property.Name).CurrentValue)
                .SequenceEqual(foreignKeyValues));
    }
}
