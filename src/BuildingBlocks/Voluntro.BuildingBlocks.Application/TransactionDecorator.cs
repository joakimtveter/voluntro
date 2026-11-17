using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Application;

/// <summary>
/// Saves a command's changes only when it succeeds (ADR-0012). On a failed result, from the handler or from the save
/// (a concurrency conflict), the changes are discarded, so a failure persists nothing even if the handler changed
/// tracked entities. An exception propagates untouched: the request ends with a 500 and the scope is thrown away.
/// </summary>
/// <remarks>Built by the module's command registrar; never registered or constructed by a module.</remarks>
public sealed class TransactionDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    IUnitOfWork unitOfWork) : ICommandHandler<TCommand, TResponse>
    where TResponse : notnull
{
    public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var result = await inner.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            unitOfWork.DiscardChanges();
            return result;
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            unitOfWork.DiscardChanges();
            return saved.Error;
        }

        return result;
    }
}

/// <inheritdoc cref="TransactionDecorator{TCommand, TResponse}"/>
public sealed class TransactionDecorator<TCommand>(
    ICommandHandler<TCommand> inner,
    IUnitOfWork unitOfWork) : ICommandHandler<TCommand>
{
    public async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var result = await inner.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            unitOfWork.DiscardChanges();
            return result;
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            unitOfWork.DiscardChanges();
        }

        return saved;
    }
}
