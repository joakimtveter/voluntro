using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Application;

/// <summary>
/// Handles one command, a request to change state. Controllers depend on this interface directly; there is no
/// mediator (ADR-0004). Expected failures come back as a failed <see cref="Result{T}"/>, not as exceptions.
/// </summary>
/// <remarks>
/// Handlers never save. They are registered through the module's command registrar, which wraps them in the
/// transaction decorator: it saves only when the result is a success, and discards the changes otherwise (ADR-0012).
/// </remarks>
/// <typeparam name="TCommand">The command, a record carrying everything the handler needs.</typeparam>
/// <typeparam name="TResponse">What a successful command returns, e.g. the new aggregate's ID.</typeparam>
public interface ICommandHandler<TCommand, TResponse>
    where TResponse : notnull
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Handles one command that returns nothing on success, such as an update: it can't return the new
/// <see cref="AggregateRoot{TId}.Version"/>, because the transaction decorator saves after the handler returns.
/// </summary>
/// <remarks><inheritdoc cref="ICommandHandler{TCommand, TResponse}" path="/remarks"/></remarks>
/// <typeparam name="TCommand">The command, a record carrying everything the handler needs.</typeparam>
public interface ICommandHandler<TCommand>
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
