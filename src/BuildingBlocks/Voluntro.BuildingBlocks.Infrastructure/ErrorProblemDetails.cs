using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure;

/// <summary>
/// Turns an <see cref="Error"/> into the API's ProblemDetails: status from the error type, <c>code</c>, <c>detail</c>,
/// and for a <see cref="ValidationError"/> the field errors as <c>errors: { field: [codes] }</c>. The one place that
/// shape is built, for controller results and for model-binding failures alike.
/// </summary>
internal static class ErrorProblemDetails
{
    public static ObjectResult Create(HttpContext httpContext, Error error)
    {
        var statusCode = ToStatusCode(error.Type);
        var factory = httpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

        // The factory fills in type, title and status, then runs CustomizeProblemDetails (traceId).
        var problem = factory.CreateProblemDetails(httpContext, statusCode, detail: error.Detail);
        problem.Extensions["code"] = error.Code;

        if (error is ValidationError validation)
        {
            problem.Extensions["errors"] = validation.Errors
                .GroupBy(fieldError => fieldError.Field, StringComparer.Ordinal)
                .ToDictionary(
                    field => field.Key,
                    field => field.Select(fieldError => fieldError.Code).Distinct(StringComparer.Ordinal).ToArray(),
                    StringComparer.Ordinal);
        }

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" },
        };
    }

    /// <summary>
    /// The automatic 400 of <c>[ApiController]</c> when binding fails, in the same shape as a validator's: every
    /// field MVC couldn't bind gets <see cref="FieldErrorCodes.InvalidValue"/>, and MVC's English messages are dropped.
    /// </summary>
    public static IActionResult FromModelState(ActionContext context)
    {
        var fieldErrors = context.ModelState
            .Where(entry => entry.Value is { ValidationState: ModelValidationState.Invalid })
            .Select(entry => new FieldError(ToFieldPath(entry.Key), FieldErrorCodes.InvalidValue))
            .ToList();

        // Not expected: MVC only calls this for an invalid ModelState, which has at least one invalid entry.
        if (fieldErrors.Count == 0)
        {
            fieldErrors.Add(new FieldError(BodyField, FieldErrorCodes.InvalidValue));
        }

        return Create(context.HttpContext, new ValidationError(fieldErrors));
    }

    /// <summary>The field name for the request body as a whole.</summary>
    internal const string BodyField = "$";

    /// <summary>
    /// A ModelState key as the client's field path. The JSON formatter's keys are JSON paths in the client's own
    /// names (<c>$.lines[0].quantity</c>); the others are C# names (<c>PageSize</c>), camelCased per segment.
    /// The empty key (a missing or unreadable body) and <c>$</c> itself become <c>$</c>.
    /// </summary>
    internal static string ToFieldPath(string key)
    {
        if (key.Length == 0 || key == BodyField)
        {
            return BodyField;
        }

        if (key.StartsWith("$.", StringComparison.Ordinal))
        {
            return key[2..];
        }

        return string.Join('.', key.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
    }

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
