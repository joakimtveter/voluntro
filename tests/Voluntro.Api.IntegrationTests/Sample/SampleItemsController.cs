using Microsoft.AspNetCore.Mvc;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Infrastructure;

namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>
/// Depends on the handler interface directly (ADR-0004): no mediator, and "go to definition"
/// leads from here to <see cref="GetSampleItemHandler"/>.
/// </summary>
[Route("api/sample/items")]
public sealed class SampleItemsController(IQueryHandler<GetSampleItem, SampleItemDto> getItem) : ApiController
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SampleItemDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await getItem.HandleAsync(new GetSampleItem(SampleItemId.From(id)), cancellationToken);

        return result.IsFailure ? Problem(result.Error) : Ok(result.Value);
    }
}
