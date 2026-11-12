using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Voluntro.BuildingBlocks.Infrastructure;

public static class TraceIdHttpContextExtensions
{
    /// <summary>
    /// The request's W3C trace ID alone (32 hex characters), as shown in the Aspire dashboard.
    /// Used for both the <c>X-Trace-Id</c> header and the ProblemDetails <c>traceId</c>, so they always match.
    /// </summary>
    public static string GetTraceId(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Features.Get<IHttpActivityFeature>()?.Activity.TraceId.ToHexString()
            ?? context.TraceIdentifier;
    }
}
