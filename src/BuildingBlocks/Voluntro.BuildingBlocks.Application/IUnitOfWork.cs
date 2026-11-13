using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Application;

/// <summary>
/// What the transaction decorator needs from a module's DbContext: save the tracked changes, or throw them away.
/// Every module context implements it; handlers never see it (ADR-0012).
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves every tracked change in one database transaction. A concurrent edit (a stale
    /// <see cref="AggregateRoot{TId}.Version"/>) is an expected failure: <see cref="CommonErrors.ConcurrencyConflict"/>.
    /// </summary>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Stops tracking every entity, so nothing changed so far can be saved later in the same scope.</summary>
    void DiscardChanges();
}
