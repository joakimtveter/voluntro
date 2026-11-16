using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Voluntro.BuildingBlocks.Infrastructure;
using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>
/// Endpoints that exist only to exercise the error pipeline. A real module would go through a handler;
/// these call <see cref="ApiController.Problem(Error)"/> directly, or throw, so each test controls the failure.
/// </summary>
[Route("api/sample")]
public sealed class SampleDiagnosticsController : ApiController
{
    public const string RequestedErrorCode = "sample.diagnostics.requested_error";

    /// <summary>Must never reach the client: it stands in for an exception message with internal details.</summary>
    public const string SecretExceptionMessage = "Connection to db-internal-01 failed for user voluntro_admin";

    /// <summary>Returns an <see cref="Error"/> of the requested type, e.g. <c>GET /api/sample/errors/Conflict</c>.</summary>
    [HttpGet("errors/{type}")]
    public ActionResult Fail(ErrorType type) => Problem(new Error(RequestedErrorCode, type));

    /// <summary>The same, with the type bound from the query string: <c>GET /api/sample/errors?type=Conflict</c>.</summary>
    [HttpGet("errors")]
    public ActionResult FailFromQuery([FromQuery] ErrorType type) => Problem(new Error(RequestedErrorCode, type));

    /// <summary>A body that is a JSON array, so a binding error's path starts at an index: <c>[1].quantity</c>.</summary>
    [HttpPost("bodies/array")]
    public ActionResult ArrayBody([FromBody] IReadOnlyList<SampleQuantity> lines) => NoContent();

    /// <summary>A body with keys of the client's choosing, so a key that isn't a plain name comes in brackets.</summary>
    [HttpPost("bodies/dictionary")]
    public ActionResult DictionaryBody([FromBody] IReadOnlyDictionary<string, int> values) => NoContent();

    /// <summary>A bug: an unhandled exception.</summary>
    [HttpGet("boom")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "MVC actions must be instance methods.")]
    public ActionResult Boom() => throw new InvalidOperationException(SecretExceptionMessage);
}

/// <summary>An element of <see cref="SampleDiagnosticsController.ArrayBody"/>'s body.</summary>
public sealed record SampleQuantity(int Quantity);
