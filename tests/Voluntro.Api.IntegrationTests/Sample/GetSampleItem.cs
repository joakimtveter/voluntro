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
        // `with` adds a developer-facing detail to the catalog error; it holds an ID, never personal data.
        Result<SampleItemDto> result = item is null
            ? SampleErrors.ItemNotFound with { Detail = $"Sample item {query.Id} does not exist." }
            : item;
        return Task.FromResult(result);
    }
}
