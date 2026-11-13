using Voluntro.BuildingBlocks.Application;
using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

public sealed record CreateSampleOrder(string CustomerName, string? Note);

public sealed class CreateSampleOrderHandler(ISampleDbContext db) : ICommandHandler<CreateSampleOrder, SampleOrderId>
{
    public async Task<Result<SampleOrderId>> HandleAsync(CreateSampleOrder command, CancellationToken cancellationToken)
    {
        var order = SampleOrder.Create(command.CustomerName, command.Note);
        db.Orders.Add(order);

        // Until the transaction decorator saves after a successful handler, the handler saves its own work.
        await db.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}
