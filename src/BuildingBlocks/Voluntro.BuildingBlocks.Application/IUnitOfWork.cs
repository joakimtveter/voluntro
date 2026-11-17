using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Application;

/// <summary>
/// What the transaction decorator needs from a module's persistence: save the tracked changes, or throw them away.
/// Each module has one, over its own DbContext; handlers never see it (ADR-0012).
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Runs the handlers of the domain events raised so far (ADR-0013), then saves every tracked change in one database
    /// transaction. A handler's failure is returned and nothing is saved. A concurrent edit (a stale
    /// <see cref="AggregateRoot{TId}.Version"/>) is an expected failure: <see cref="CommonErrors.ConcurrencyConflict"/>.
    /// </summary>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Stops tracking every entity, so nothing changed so far can be saved later in the same scope.</summary>
    void DiscardChanges();
}
