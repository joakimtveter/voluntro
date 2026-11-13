using Voluntro.BuildingBlocks.Application;
using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>The body of <c>PUT /api/sample/orders/{id}</c>; the ID comes from the route.</summary>
public sealed record UpdateSampleOrderRequest(string CustomerName, string? Note, uint Version);

public sealed record UpdateSampleOrder(SampleOrderId Id, string CustomerName, string? Note, uint Version);

public sealed class UpdateSampleOrderValidator : IValidator<UpdateSampleOrder>
{
    public IReadOnlyList<FieldError> Validate(UpdateSampleOrder instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var errors = new List<FieldError>();

        if (string.IsNullOrWhiteSpace(instance.CustomerName))
        {
            errors.Add(new FieldError("customerName", FieldErrorCodes.Required));
        }
        else if (instance.CustomerName.Length > CreateSampleOrderValidator.CustomerNameMaxLength)
        {
            errors.Add(new FieldError("customerName", FieldErrorCodes.TooLong));
        }

        return errors;
    }
}

/// <summary>The shape of every update: load, check the version, change through the aggregate, no save.</summary>
public sealed class UpdateSampleOrderHandler(ISampleDbContext db) : ICommandHandler<UpdateSampleOrder>
{
    public async Task<Result> HandleAsync(UpdateSampleOrder command, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FindAsync([command.Id], cancellationToken);
        if (order is null)
        {
            return SampleErrors.OrderNotFound with { Detail = $"Sample order {command.Id} does not exist." };
        }

        var versionCheck = order.EnsureVersion(command.Version);
        if (versionCheck.IsFailure)
        {
            return versionCheck;
        }

        order.Update(command.CustomerName, command.Note);
        return Result.Success();
    }
}
