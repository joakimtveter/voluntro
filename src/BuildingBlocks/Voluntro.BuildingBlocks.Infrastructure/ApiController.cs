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
    /// <summary>Turns an expected failure into an RFC 9457 ProblemDetails response.</summary>
    protected ActionResult Problem(Error error) =>
        throw new NotImplementedException("Slice 2: map the Error to a ProblemDetails response.");
}
