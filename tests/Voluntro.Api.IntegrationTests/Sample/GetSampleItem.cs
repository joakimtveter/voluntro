using Voluntro.BuildingBlocks.Application;
using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

public sealed record GetSampleItem(SampleItemId Id);

public sealed class GetSampleItemHandler : IQueryHandler<GetSampleItem, SampleItemDto>
{
    public Task<Result<SampleItemDto>> HandleAsync(GetSampleItem query, CancellationToken cancellationToken)
    {
        var item = SampleItems.All.FirstOrDefault(i => i.Id == query.Id.Value);

        // The implicit conversions from SharedKernel turn an Error or a value into a Result.
        Result<SampleItemDto> result = item is null ? SampleErrors.ItemNotFound : item;
        return Task.FromResult(result);
    }
}
