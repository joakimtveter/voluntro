namespace Voluntro.SharedKernel;

/// <summary>
/// A request whose shape is wrong, with one <see cref="FieldError"/> per problem. Returned by the validation
/// decorator and by model binding; the API answers 400 with <c>errors: { field: [codes] }</c>.
/// </summary>
public sealed record ValidationError : Error
{
    public const string ErrorCode = "common.validation_failed";

    /// <exception cref="ArgumentException"><paramref name="errors"/> is empty or contains null.</exception>
    public ValidationError(IEnumerable<FieldError> errors)
        : base(ErrorCode, ErrorType.Validation)
    {
        ArgumentNullException.ThrowIfNull(errors);

        FieldError[] copy = [.. errors];
        if (copy.Length == 0)
        {
            throw new ArgumentException("A validation error needs at least one field error.", nameof(errors));
        }

        if (copy.Any(error => error is null))
        {
            throw new ArgumentException("Field errors can't be null.", nameof(errors));
        }

        Errors = copy;
    }

    /// <summary>The field errors, in the order they were found. Never empty.</summary>
    public IReadOnlyList<FieldError> Errors { get; }

    // A record compares a list by reference; two validation errors are equal when their field errors are.
    public bool Equals(ValidationError? other) => base.Equals(other) && Errors.SequenceEqual(other.Errors);

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Errors.Count);
}
