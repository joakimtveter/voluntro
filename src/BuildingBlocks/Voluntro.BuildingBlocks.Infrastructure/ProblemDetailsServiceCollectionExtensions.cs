using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Voluntro.BuildingBlocks.Infrastructure;

public static class ProblemDetailsServiceCollectionExtensions
{
    /// <summary>Code of the generic 500 returned for an unhandled exception (a bug).</summary>
    public const string UnexpectedErrorCode = "common.unexpected";

    /// <summary>
    /// Registers RFC 9457 ProblemDetails for every error response: from <see cref="ApiController"/>,
    /// from the exception handler and from status-code pages. Call it after <c>AddControllers()</c>.
    /// </summary>
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
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

        // [ApiController]'s automatic 400 for a request that doesn't bind answers in the validator's shape:
        // code common.validation_failed and errors: { field: [codes] }, never MVC's English messages.
        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = ErrorProblemDetails.FromModelState);

        // Request shape is the validators' job. MVC would otherwise treat every non-nullable reference property as
        // [Required] and answer a missing customerName with invalid_value before the validator could say required.
        services.Configure<MvcOptions>(options =>
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

        // Writers are asked in registration order and the first that can write wins, so this one must come last:
        // after MVC's (controller endpoints) and the framework's default (clients that accept JSON).
        services.AddSingleton<IProblemDetailsWriter, AnyAcceptProblemDetailsWriter>();

        return services;
    }
}

/// <summary>
/// Writes ProblemDetails as JSON when no other writer will, so errors have one format whatever the client asked for.
/// </summary>
/// <remarks>
/// The framework's default writer refuses a request whose <c>Accept</c> header leaves out JSON (a browser's
/// <c>text/html</c>, say). Status-code pages then fall back to a <c>text/plain</c> line with no <c>traceId</c>,
/// and the exception handler to an empty 500. This writer does what the default one does, without that check.
/// </remarks>
internal sealed class AnyAcceptProblemDetailsWriter(
    IOptions<ProblemDetailsOptions> problemDetailsOptions,
    IOptions<ApiBehaviorOptions> apiBehaviorOptions,
    IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions> jsonOptions) : IProblemDetailsWriter
{
    public bool CanWrite(ProblemDetailsContext context) => true;

    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var response = context.HttpContext.Response;
        var problem = context.ProblemDetails;

        // The standard title and type link for the status, the same ones ApiController.Problem gets from MVC.
        problem.Status ??= response.StatusCode;
        if (apiBehaviorOptions.Value.ClientErrorMapping.TryGetValue(problem.Status.Value, out var standard))
        {
            problem.Title ??= standard.Title;
            problem.Type ??= standard.Link;
        }

        // traceId, and code for an unhandled exception (see AddApiProblemDetails).
        problemDetailsOptions.Value.CustomizeProblemDetails?.Invoke(context);

        // The runtime type, so a subclass such as ValidationProblemDetails keeps its extra members.
        return new ValueTask(response.WriteAsJsonAsync(
            problem,
            problem.GetType(),
            jsonOptions.Value.SerializerOptions,
            contentType: "application/problem+json",
            context.HttpContext.RequestAborted));
    }
}
