using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure;

/// <summary>
/// Base class for every module controller. Actions return <c>ActionResult&lt;TDto&gt;</c> and branch explicitly:
/// <c>return result.IsFailure ? Problem(result.Error) : Ok(result.Value);</c>
/// </summary>
[ApiController]
public abstract class ApiController : ControllerBase
{
    /// <summary>
    /// Turns an expected failure into an RFC 9457 ProblemDetails response carrying the error's <c>code</c>,
    /// if set its developer-facing <c>detail</c>, and for a <see cref="ValidationError"/> its field errors as
    /// <c>errors: { field: [codes] }</c>.
    /// </summary>
    protected ActionResult Problem(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return ErrorProblemDetails.Create(HttpContext, error);
    }

    /// <summary>
    /// <c>201 Created</c> for a new resource: <c>Location</c> points at <paramref name="actionName"/>, the controller's
    /// get-by-ID action (its route parameter must be named <c>id</c>), and the body is <c>{ id }</c>.
    /// Declare <c>[ProducesResponseType&lt;CreatedResponse&gt;(StatusCodes.Status201Created)]</c> on the action.
    /// </summary>
    /// <remarks>
    /// Not the full resource: the transaction decorator saves after the handler returns, so the handler can't know
    /// the new <c>version</c>, and returning the resource would take a second read.
    /// </remarks>
    protected CreatedAtActionResult CreatedAt(string actionName, Guid id) =>
        CreatedAtAction(actionName, new { id }, new CreatedResponse(id));
}
