using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Voluntro.BuildingBlocks.Infrastructure;

public static class ProblemDetailsServiceCollectionExtensions
{
    /// <summary>Code of the generic 500 returned for an unhandled exception (a bug).</summary>
    public const string UnexpectedErrorCode = "common.unexpected";

    /// <summary>
    /// Registers RFC 9457 ProblemDetails for every error response: from <see cref="ApiController"/>,
    /// from the exception handler and from status-code pages.
    /// </summary>
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services) =>
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            // Same value as the X-Trace-Id header; the default is the full W3C "00-…-…-01" traceparent.
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.GetTraceId();

            // An exception is a bug. Say only that something went wrong; details go to the log, never to the client.
            // Read the exception from the request, not from context.Exception: for a controller endpoint MVC writes
            // the response through ProblemDetailsFactory, which passes a fresh context without the exception.
            if (context.HttpContext.Features.Get<IExceptionHandlerFeature>() is not null)
            {
                context.ProblemDetails.Extensions["code"] = UnexpectedErrorCode;
            }
        });
}
