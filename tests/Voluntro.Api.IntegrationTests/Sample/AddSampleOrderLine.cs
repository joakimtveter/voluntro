using Microsoft.EntityFrameworkCore;
using Voluntro.BuildingBlocks.Application;
using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>The body of <c>POST /api/sample/orders/{id}/lines</c>; the order's ID comes from the route.</summary>
public sealed record AddSampleOrderLineRequest(string ProductName, int Quantity, uint Version);

public sealed record AddSampleOrderLine(SampleOrderId OrderId, string ProductName, int Quantity, uint Version);

public sealed class AddSampleOrderLineValidator : IValidator<AddSampleOrderLine>
{
    /// <summary>The same limit as the <c>product_name</c> column.</summary>
    public const int ProductNameMaxLength = 200;

    public const int MaxQuantity = 1000;

    public IReadOnlyList<FieldError> Validate(AddSampleOrderLine instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var errors = new List<FieldError>();

        if (string.IsNullOrWhiteSpace(instance.ProductName))
        {
            errors.Add(new FieldError("productName", FieldErrorCodes.Required));
        }
        else if (instance.ProductName.Length > ProductNameMaxLength)
        {
            errors.Add(new FieldError("productName", FieldErrorCodes.TooLong));
        }

        if (instance.Quantity is < 1 or > MaxQuantity)
        {
            errors.Add(new FieldError("quantity", FieldErrorCodes.OutOfRange));
        }

        return errors;
    }
}

/// <summary>
/// A change to a child entity alone. The version check covers the whole aggregate, and the audit interceptor stamps
/// the order, so its row is written and a concurrent edit is caught on save too.
/// </summary>
public sealed class AddSampleOrderLineHandler(ISampleDbContext db) : ICommandHandler<AddSampleOrderLine, SampleOrderLineId>
{
    public async Task<Result<SampleOrderLineId>> HandleAsync(AddSampleOrderLine command, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);
        if (order is null)
        {
            return SampleErrors.OrderNotFound with { Detail = $"Sample order {command.OrderId} does not exist." };
        }

        var versionCheck = order.EnsureVersion(command.Version);
        if (versionCheck.IsFailure)
        {
            return versionCheck.Error;
        }

        return order.AddLine(command.ProductName, command.Quantity).Id;
    }
}
