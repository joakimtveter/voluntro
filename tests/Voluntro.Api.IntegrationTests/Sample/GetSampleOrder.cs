using Microsoft.EntityFrameworkCore;
using Voluntro.BuildingBlocks.Application;
using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

public sealed record GetSampleOrder(SampleOrderId Id);

/// <summary>Projects straight from EF to the DTO, untracked, without loading the aggregate (ADR-0010).</summary>
public sealed class GetSampleOrderHandler(ISampleDbContext db) : IQueryHandler<GetSampleOrder, SampleOrderDto>
{
    public async Task<Result<SampleOrderDto>> HandleAsync(GetSampleOrder query, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Where(o => o.Id == query.Id)
            .Select(o => new SampleOrderDto(o.Id.Value, o.CustomerName, o.Status, o.Note, o.Version))
            .SingleOrDefaultAsync(cancellationToken);

        return order is null
            ? SampleErrors.OrderNotFound with { Detail = $"Sample order {query.Id} does not exist." }
            : order;
    }
}

/// <summary>An order as the API returns it. <see cref="Version"/> is what an update will have to send back.</summary>
public sealed record SampleOrderDto(Guid Id, string CustomerName, SampleOrderStatus Status, string? Note, uint Version);
