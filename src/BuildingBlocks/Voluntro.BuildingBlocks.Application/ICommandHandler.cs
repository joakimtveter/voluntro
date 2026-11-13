using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Application;

/// <summary>
/// Handles one command, a request to change state. Controllers depend on this interface directly; there is no
/// mediator (ADR-0004). Expected failures come back as a failed <see cref="Result{T}"/>, not as exceptions.
/// </summary>
/// <remarks>
/// Until the transaction decorator owns saving, a handler saves its own changes. Once it does, handlers never save:
/// the decorator saves only when the result is a success.
/// </remarks>
/// <typeparam name="TCommand">The command, a record carrying everything the handler needs.</typeparam>
/// <typeparam name="TResponse">What a successful command returns, e.g. the new aggregate's ID.</typeparam>
public interface ICommandHandler<TCommand, TResponse>
    where TResponse : notnull
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
