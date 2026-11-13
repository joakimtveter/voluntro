using System.Text.RegularExpressions;

namespace Voluntro.SharedKernel;

/// <summary>
/// An expected failure. <see cref="Code"/> is stable, owned by a module and dot-separated
/// (<c>membership.person.not_found</c>); the frontend translates it. Modules declare their errors
/// in catalogs next to the aggregate, e.g.
/// <c>public static class PersonErrors { public static readonly Error NotFound = Error.NotFound("membership.person.not_found"); }</c>.
/// </summary>
/// <remarks>
/// Not sealed for exactly one subtype, <see cref="ValidationError"/>, which carries field-level errors. Don't
/// derive from it otherwise: code and type are the whole contract, and a new kind of failure is a new
/// <see cref="ErrorType"/>, not a subclass.
/// </remarks>
public partial record Error
{
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="code"/> is not lowercase snake_case segments separated by dots.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> is not a defined <see cref="ErrorType"/>.</exception>
    public Error(string code, ErrorType type, string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (!CodeFormat().IsMatch(code))
        {
            throw new ArgumentException(
                $"Error code '{code}' must be two or more lowercase snake_case segments separated by dots, e.g. 'membership.person.not_found'.",
                nameof(code));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Undefined error type.");
        }

        Code = code;
        Type = type;
        Detail = detail;
    }

    /// <summary>Stable, dot-separated code, e.g. <c>membership.person.not_found</c>.</summary>
    public string Code { get; }

    public ErrorType Type { get; }

    /// <summary>Optional developer-facing detail. Never shown untranslated to end users; never personal data.</summary>
    public string? Detail { get; init; }

    public static Error Failure(string code, string? detail = null) => new(code, ErrorType.Failure, detail);

    public static Error Validation(string code, string? detail = null) => new(code, ErrorType.Validation, detail);

    public static Error NotFound(string code, string? detail = null) => new(code, ErrorType.NotFound, detail);

    public static Error Conflict(string code, string? detail = null) => new(code, ErrorType.Conflict, detail);

    public static Error Forbidden(string code, string? detail = null) => new(code, ErrorType.Forbidden, detail);

    public static Error Unauthorized(string code, string? detail = null) => new(code, ErrorType.Unauthorized, detail);

    // \z rather than $: $ also matches before a trailing newline.
    [GeneratedRegex(@"^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+\z", RegexOptions.CultureInvariant)]
    private static partial Regex CodeFormat();
}
