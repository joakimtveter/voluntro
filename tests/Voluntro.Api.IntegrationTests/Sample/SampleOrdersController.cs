using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Infrastructure;

namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>The persisted sample aggregate: create through a command handler, read back through a query handler.</summary>
[Route("api/sample/orders")]
public sealed class SampleOrdersController(
    ICommandHandler<CreateSampleOrder, SampleOrderId> createOrder,
    IQueryHandler<GetSampleOrder, SampleOrderDto> getOrder) : ApiController
{
    /// <summary><c>POST /api/sample/orders</c>: 201 with the new order's address in <c>Location</c> and <c>{ id }</c>.</summary>
    [HttpPost]
    [ProducesResponseType<CreatedResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CreatedResponse>> Create(CreateSampleOrder command, CancellationToken cancellationToken)
    {
        var result = await createOrder.HandleAsync(command, cancellationToken);

        return result.IsFailure ? Problem(result.Error) : CreatedAt(nameof(Get), result.Value.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<SampleOrderDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await getOrder.HandleAsync(new GetSampleOrder(SampleOrderId.From(id)), cancellationToken);

        return result.IsFailure ? Problem(result.Error) : Ok(result.Value);
    }
}
