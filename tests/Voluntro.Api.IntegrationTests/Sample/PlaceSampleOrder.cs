using Microsoft.EntityFrameworkCore;
using Voluntro.BuildingBlocks.Application;
using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

public sealed record PlaceSampleOrder(SampleOrderId Id);

/// <summary>
/// Exists to prove that a failed command persists nothing, even when its handler already changed tracked entities.
/// </summary>
public sealed class PlaceSampleOrderHandler(ISampleDbContext db) : ICommandHandler<PlaceSampleOrder>
{
    public async Task<Result> HandleAsync(PlaceSampleOrder command, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == command.Id, cancellationToken);

        if (order is null)
        {
            return SampleErrors.OrderNotFound with { Detail = $"Sample order {command.Id} does not exist." };
        }

        // Deliberately the wrong way round: change first, check second. A real aggregate checks before changing
        // anything; here the order is already Placed in the change tracker when the failure is returned, and the
        // transaction decorator must still persist nothing.
        order.Place();

        return order.Lines.Count == 0 ? SampleErrors.OrderEmpty : Result.Success();
    }
}
