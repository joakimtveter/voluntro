using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Application;

/// <summary>
/// Runs the command's validator first and returns a <see cref="ValidationError"/> without calling anything further
/// when it finds problems. Outermost in the pipeline, so invalid input never reaches the transaction or the handler
/// (ADR-0012).
/// </summary>
/// <remarks>Built by the module's command registrar; never registered or constructed by a module.</remarks>
public sealed class ValidationDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    IValidator<TCommand> validator) : ICommandHandler<TCommand, TResponse>
    where TResponse : notnull
{
    public Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var errors = validator.Validate(command);

        return errors.Count > 0
            ? Task.FromResult<Result<TResponse>>(new ValidationError(errors))
            : inner.HandleAsync(command, cancellationToken);
    }
}

/// <inheritdoc cref="ValidationDecorator{TCommand, TResponse}"/>
public sealed class ValidationDecorator<TCommand>(
    ICommandHandler<TCommand> inner,
    IValidator<TCommand> validator) : ICommandHandler<TCommand>
{
    public Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var errors = validator.Validate(command);

        return errors.Count > 0
            ? Task.FromResult<Result>(new ValidationError(errors))
            : inner.HandleAsync(command, cancellationToken);
    }
}
