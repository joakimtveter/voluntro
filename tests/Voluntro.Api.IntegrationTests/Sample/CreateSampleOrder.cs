using Voluntro.BuildingBlocks.Application;
using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

public sealed record CreateSampleOrder(string CustomerName, string? Note);

public sealed class CreateSampleOrderValidator : IValidator<CreateSampleOrder>
{
    /// <summary>The same limit as the <c>customer_name</c> column.</summary>
    public const int CustomerNameMaxLength = 200;

    public IReadOnlyList<FieldError> Validate(CreateSampleOrder instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var errors = new List<FieldError>();

        // Declared non-nullable, but a client can still leave it out: JSON binding doesn't enforce it.
        if (string.IsNullOrWhiteSpace(instance.CustomerName))
        {
            errors.Add(new FieldError("customerName", FieldErrorCodes.Required));
        }
        else if (instance.CustomerName.Length > CustomerNameMaxLength)
        {
            errors.Add(new FieldError("customerName", FieldErrorCodes.TooLong));
        }

        return errors;
    }
}

public sealed class CreateSampleOrderHandler(ISampleDbContext db) : ICommandHandler<CreateSampleOrder, SampleOrderId>
{
    public Task<Result<SampleOrderId>> HandleAsync(CreateSampleOrder command, CancellationToken cancellationToken)
    {
        var order = SampleOrder.Create(command.CustomerName, command.Note);
        db.Orders.Add(order);

        // No save: the transaction decorator saves after a successful handler.
        return Task.FromResult<Result<SampleOrderId>>(order.Id);
    }
}
