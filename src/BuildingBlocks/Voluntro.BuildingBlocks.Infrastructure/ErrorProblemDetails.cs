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
        var explicitNames = ExplicitNames(context);
        var fieldErrors = context.ModelState
            .Where(entry => entry.Value is { ValidationState: ModelValidationState.Invalid })
            .Select(entry => new FieldError(ToFieldPath(entry.Key, explicitNames), FieldErrorCodes.InvalidValue))
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
    /// names, and the path is what follows the root <c>$</c>: <c>$.lines[0].quantity</c> → <c>lines[0].quantity</c>,
    /// <c>$[1].quantity</c> (an array body) → <c>[1].quantity</c>, <c>$['my key']</c> → <c>['my key']</c>. The other
    /// keys are C# names (<c>PageSize</c>), camelCased per segment, except a name in
    /// <paramref name="explicitNames"/>, which is kept as declared (<c>X-Request-Id</c>). The empty key (a missing or
    /// unreadable body), <c>$</c> itself and <c>$.</c> (a dictionary key that is the empty string) become <c>$</c>.
    /// </summary>
    internal static string ToFieldPath(string key, IReadOnlySet<string> explicitNames)
    {
        if (key.Length == 0)
        {
            return BodyField;
        }

        if (IsJsonPath(key))
        {
            var path = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key[1..];

            // Never empty: FieldError rejects an empty field, which would turn a malformed request into a 500.
            return path.Length == 0 ? BodyField : path;
        }

        // An explicit name may hold a dot itself, so the whole key is tried before its segments.
        return explicitNames.Contains(key)
            ? key
            : string.Join('.', key.Split('.').Select(segment => ToFieldName(segment, explicitNames)));
    }

    // "$", "$.name", "$[0]", "$['my key']". A query parameter may start with $ too ($top), and is no path.
    private static bool IsJsonPath(string key) => key[0] == '$' && (key.Length == 1 || key[1] is '.' or '[');

    // A segment may end in an index: "Lines[0]" is the explicit name "Lines".
    private static string ToFieldName(string segment, IReadOnlySet<string> explicitNames)
    {
        var index = segment.IndexOf('[', StringComparison.Ordinal);
        var name = index < 0 ? segment : segment[..index];

        return explicitNames.Contains(name) ? segment : JsonNamingPolicy.CamelCase.ConvertName(segment);
    }

    /// <summary>
    /// The names the action's parameters were given explicitly, e.g. <c>[FromQuery(Name = "q")]</c> or
    /// <c>[FromHeader(Name = "X-Request-Id")]</c>: on a parameter itself, or on a property of a parameter's type.
    /// They are what the client sent and what the OpenAPI document lists
    /// (<see cref="CamelCaseQueryParameterNames"/> leaves them alone too), so they are never camelCased.
    /// </summary>
    private static HashSet<string> ExplicitNames(ActionContext context)
    {
        var metadataProvider = context.HttpContext.RequestServices.GetRequiredService<IModelMetadataProvider>();
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var parameter in context.ActionDescriptor.Parameters.Concat(context.ActionDescriptor.BoundProperties))
        {
            if (parameter.BindingInfo?.BinderModelName is { } parameterName)
            {
                names.Add(parameterName);
            }

            // The body's keys are JSON paths, in the client's names already.
            if (parameter.BindingInfo?.BindingSource == BindingSource.Body)
            {
                continue;
            }

            foreach (var property in metadataProvider.GetMetadataForType(parameter.ParameterType).Properties)
            {
                if (property.BinderModelName is { } propertyName)
                {
                    names.Add(propertyName);
                }
            }
        }

        return names;
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
