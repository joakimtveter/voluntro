using System.Text.RegularExpressions;

namespace Voluntro.SharedKernel;

/// <summary>
/// One problem with one field of a request, part of a <see cref="ValidationError"/>. The frontend maps
/// <see cref="Field"/> onto its form and translates <see cref="Code"/>.
/// </summary>
public sealed partial record FieldError
{
    /// <exception cref="ArgumentException">
    /// <paramref name="field"/> is empty, or <paramref name="code"/> is not one lowercase snake_case word.
    /// </exception>
    public FieldError(string field, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        ArgumentNullException.ThrowIfNull(code);

        if (!CodeFormat().IsMatch(code))
        {
            throw new ArgumentException(
                $"Field error code '{code}' must be lowercase snake_case without dots, e.g. 'too_long'.",
                nameof(code));
        }

        Field = field;
        Code = code;
    }

    /// <summary>
    /// The field's path as the client sent it, in camelCase: <c>customerName</c>, <c>lines[0].quantity</c>.
    /// <c>$</c> is the request body as a whole (missing or unreadable).
    /// </summary>
    public string Field { get; }

    /// <summary>A short code reused across fields, e.g. <see cref="FieldErrorCodes.Required"/>.</summary>
    public string Code { get; }

    // \z rather than $: $ also matches before a trailing newline.
    [GeneratedRegex(@"^[a-z][a-z0-9_]*\z", RegexOptions.CultureInvariant)]
    private static partial Regex CodeFormat();
}

/// <summary>
/// The field error codes. Generic and argument-free: the frontend's form schema already knows the limits.
/// </summary>
public static class FieldErrorCodes
{
    /// <summary>Missing, null, or only whitespace.</summary>
    public const string Required = "required";

    /// <summary>Longer than the field allows.</summary>
    public const string TooLong = "too_long";

    /// <summary>A number or date outside the allowed range.</summary>
    public const string OutOfRange = "out_of_range";

    /// <summary>Present, but not in the expected format (an email address, a phone number, …).</summary>
    public const string InvalidFormat = "invalid_format";

    /// <summary>
    /// The value couldn't be read at all: the wrong JSON type, an unparsable query value, an unreadable body.
    /// Produced by model binding, before any validator runs.
    /// </summary>
    public const string InvalidValue = "invalid_value";
}
