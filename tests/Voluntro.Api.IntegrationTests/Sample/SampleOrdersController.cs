using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Infrastructure;

namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>The persisted sample aggregate: create through a command handler, read back through a query handler.</summary>
[Route("api/sample/orders")]
public sealed class SampleOrdersController(
    ICommandHandler<CreateSampleOrder, SampleOrderId> createOrder,
    ICommandHandler<UpdateSampleOrder> updateOrder,
    ICommandHandler<AddSampleOrderLine, SampleOrderLineId> addLine,
    ICommandHandler<PlaceSampleOrder> placeOrder,
    IQueryHandler<GetSampleOrder, SampleOrderDto> getOrder) : ApiController
{
    /// <summary><c>POST /api/sample/orders</c>: 201 with the new order's address in <c>Location</c> and <c>{ id }</c>.</summary>
    [HttpPost]
    [ProducesResponseType<CreatedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<CreatedResponse>> Create(CreateSampleOrder command, CancellationToken cancellationToken)
    {
        var result = await createOrder.HandleAsync(command, cancellationToken);

        return result.IsFailure ? Problem(result.Error) : CreatedAt(nameof(Get), result.Value.Value);
    }

    /// <summary>
    /// <c>PUT /api/sample/orders/{id}</c> with <c>{ customerName, note, version }</c>: 204, or 409
    /// <c>common.concurrency_conflict</c> when the order changed since the client read <c>version</c>.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult> Update(Guid id, UpdateSampleOrderRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new UpdateSampleOrder(SampleOrderId.From(id), request.CustomerName, request.Note, request.Version);
        var result = await updateOrder.HandleAsync(command, cancellationToken);

        return result.IsFailure ? Problem(result.Error) : NoContent();
    }

    /// <summary>
    /// <c>POST /api/sample/orders/{id}/lines</c> with <c>{ productName, quantity, version }</c>: 201 with the new
    /// line's <c>{ id }</c>. A line has no address of its own, so <c>Location</c> is its order.
    /// </summary>
    [HttpPost("{id:guid}/lines")]
    [ProducesResponseType<CreatedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CreatedResponse>> AddLine(Guid id, AddSampleOrderLineRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new AddSampleOrderLine(SampleOrderId.From(id), request.ProductName, request.Quantity, request.Version);
        var result = await addLine.HandleAsync(command, cancellationToken);

        return result.IsFailure ? Problem(result.Error) : CreatedAt(nameof(Get), new { id }, result.Value.Value);
    }

    /// <summary><c>POST /api/sample/orders/{id}/place</c>: 204, or 409 <c>sample.order.empty</c> for an order without lines.</summary>
    [HttpPost("{id:guid}/place")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult> Place(Guid id, CancellationToken cancellationToken)
    {
        var result = await placeOrder.HandleAsync(new PlaceSampleOrder(SampleOrderId.From(id)), cancellationToken);

        return result.IsFailure ? Problem(result.Error) : NoContent();
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
