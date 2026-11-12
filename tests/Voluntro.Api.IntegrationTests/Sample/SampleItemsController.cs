using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Contracts;
using Voluntro.BuildingBlocks.Infrastructure;

namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>
/// Depends on the handler interface directly (ADR-0004): no mediator, and "go to definition"
/// leads from here to <see cref="GetSampleItemHandler"/>.
/// </summary>
[Route("api/sample/items")]
public sealed class SampleItemsController(
    IQueryHandler<GetSampleItem, SampleItemDto> getItem,
    IQueryHandler<ListSampleItems, PagedList<SampleItemDto>> listItems) : ApiController
{
    /// <summary><c>GET /api/sample/items?page=1&amp;pageSize=25&amp;sort=…&amp;search=…</c></summary>
    [HttpGet]
    public async Task<ActionResult<PagedList<SampleItemDto>>> List([FromQuery] PageRequest paging, CancellationToken cancellationToken)
    {
        var result = await listItems.HandleAsync(new ListSampleItems(paging), cancellationToken);

        return result.IsFailure ? Problem(result.Error) : Ok(result.Value);
    }

    // Declaring any response replaces the 200 MVC infers from the return type, so the 200 is declared too;
    // its body type still comes from ActionResult<SampleItemDto>.
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<SampleItemDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await getItem.HandleAsync(new GetSampleItem(SampleItemId.From(id)), cancellationToken);

        return result.IsFailure ? Problem(result.Error) : Ok(result.Value);
    }
}
