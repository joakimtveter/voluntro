using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Application;

/// <summary>
/// Handles one query. Controllers depend on this interface directly; there is no mediator (ADR-0004).
/// Expected failures, such as a missing item, come back as a failed <see cref="Result{T}"/>, not as exceptions.
/// </summary>
/// <typeparam name="TQuery">The query, a record carrying everything the handler needs.</typeparam>
/// <typeparam name="TResponse">What a successful query returns. Never null: "not found" is an error.</typeparam>
public interface IQueryHandler<TQuery, TResponse>
    where TResponse : notnull
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
