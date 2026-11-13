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
    /// Turns an expected failure into an RFC 9457 ProblemDetails response carrying the error's <c>code</c>
    /// and, if set, its developer-facing <c>detail</c>.
    /// </summary>
    protected ActionResult Problem(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var statusCode = ToStatusCode(error.Type);

        // The factory fills in type, title and status, then runs CustomizeProblemDetails (traceId).
        var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, statusCode, detail: error.Detail);
        problem.Extensions["code"] = error.Code;

        // MVC writes a ProblemDetails value as application/problem+json.
        return new ObjectResult(problem) { StatusCode = statusCode };
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

    private static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Failure => StatusCodes.Status500InternalServerError,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "No HTTP status is mapped for this error type."),
    };
}
