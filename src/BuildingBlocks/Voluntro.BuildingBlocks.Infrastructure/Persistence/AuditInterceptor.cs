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
/// has no such navigation and is never followed. Saving throws on a model or a change it can't stamp correctly: a
/// child with no owning navigation or more than one, a child moved to another owner, or a child changed without
/// loading its aggregate. Only <see cref="Entity{TId}"/> children stamp their root: rows that aren't entities (owned
/// types in a table of their own, many-to-many join rows) don't (ADR-0011).
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
            var modifiedAt = root.Property(nameof(AggregateRoot<>.ModifiedAt));
            modifiedAt.CurrentValue = now;
            root.Property(nameof(AggregateRoot<>.ModifiedBy)).CurrentValue = userId;

            // Assigning the value a property already has marks nothing (same user, a stopped clock in a test), and the
            // root's row must be written whatever the values: that write carries the xmin check.
            modifiedAt.IsModified = true;
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
    /// The entity has no owner in the model or more than one, it moved to another owner, or its owner isn't tracked.
    /// </exception>
    private static EntityEntry FindRoot(EntityEntry child, IReadOnlyList<EntityEntry> entries)
    {
        var current = child;

        while (!AggregateRoots.IsAggregateRoot(current.Metadata.ClrType))
        {
            var ownership = FindOwnership(current.Metadata);

            // Only the owner the child has now is found and stamped. The aggregate it left would change without a
            // new version or an xmin check, so a concurrent edit of that aggregate would go unnoticed.
            if (current.State == EntityState.Modified
                && ownership.Properties.Any(property => current.Property(property.Name).IsModified))
            {
                throw new InvalidOperationException(
                    $"A {current.Metadata.ClrType.Name} moved to another {ownership.PrincipalEntityType.ClrType.Name}: " +
                    "its owning foreign key changed. Moving a child between aggregates isn't supported, because the " +
                    "aggregate it left wouldn't be stamped or version-checked. Remove it from one aggregate and add a " +
                    "new child to the other.");
            }

            current = FindPrincipal(current, ownership, entries)
                ?? throw new InvalidOperationException(
                    $"A {child.Metadata.ClrType.Name} changed, but its {ownership.PrincipalEntityType.ClrType.Name} " +
                    "isn't tracked. Load the aggregate and change the child through it, so the root's version is " +
                    "checked and its audit stamp updated.");
        }

        return current;
    }

    /// <summary>The one foreign key whose principal has a navigation to this entity: the way up to its owner.</summary>
    private static IForeignKey FindOwnership(IEntityType entityType)
    {
        var owners = entityType.GetForeignKeys().Where(foreignKey => foreignKey.PrincipalToDependent is not null).ToList();

        return owners.Count switch
        {
            1 => owners[0],
            0 => throw new InvalidOperationException(
                $"{entityType.ClrType.Name} is an entity but no aggregate owns it: none of its foreign keys " +
                "has a navigation from the principal (HasMany(root => root.Children)). Configure it as a child " +
                "of its aggregate root, or make it an aggregate root itself."),
            _ => throw new InvalidOperationException(
                $"{entityType.ClrType.Name} has more than one owning navigation " +
                $"({string.Join(", ", owners.Select(foreignKey => $"{foreignKey.PrincipalEntityType.ClrType.Name}.{foreignKey.PrincipalToDependent!.Name}"))}), " +
                "so its aggregate root is ambiguous. Keep exactly one, and refer to the others by ID without a " +
                "collection navigation."),
        };
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
