using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Contracts;
using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

public sealed record ListSampleItems(PageRequest Paging);

/// <summary>
/// Pages the in-memory list. A real module would page an EF query instead; that extension is designed with
/// the first real list endpoint, together with the sort grammar (Sort is ignored here).
/// </summary>
public sealed class ListSampleItemsHandler : IQueryHandler<ListSampleItems, PagedList<SampleItemDto>>
{
    public Task<Result<PagedList<SampleItemDto>>> HandleAsync(ListSampleItems query, CancellationToken cancellationToken)
    {
        var paging = query.Paging;

        var matching = string.IsNullOrWhiteSpace(paging.Search)
            ? SampleItems.All
            : SampleItems.All.Where(item => item.Name.Contains(paging.Search, StringComparison.OrdinalIgnoreCase)).ToList();

        var page = matching
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToList();

        Result<PagedList<SampleItemDto>> result = new PagedList<SampleItemDto>(page, paging.Page, paging.PageSize, matching.Count);
        return Task.FromResult(result);
    }
}
