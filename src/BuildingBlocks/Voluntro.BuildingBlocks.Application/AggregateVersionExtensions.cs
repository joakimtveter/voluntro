using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Application;

public static class AggregateVersionExtensions
{
    /// <summary>
    /// Checks that the aggregate is still the version the client read: the first line of every update handler after
    /// loading it (ADR-0011). An edit between this check and the save is caught by the <c>xmin</c> check on save, which
    /// the transaction decorator turns into the same <see cref="CommonErrors.ConcurrencyConflict"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// var versionCheck = order.EnsureVersion(command.Version);
    /// if (versionCheck.IsFailure) return versionCheck;
    /// </code>
    /// </example>
    public static Result EnsureVersion<TId>(this AggregateRoot<TId> aggregate, uint expectedVersion)
        where TId : struct, IEntityId<TId>
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        // Equality only: xmin wraps around, so a larger version isn't necessarily newer.
        return aggregate.Version == expectedVersion
            ? Result.Success()
            : CommonErrors.ConcurrencyConflict with
            {
                Detail = $"{aggregate.GetType().Name} {aggregate.Id} is at version {aggregate.Version}, not {expectedVersion}.",
            };
    }
}
