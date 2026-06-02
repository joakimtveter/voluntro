using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Validation;

/// <summary>
/// Validation attribute that rejects dates in the future.
/// </summary>
public class NotInFutureAttribute : ValidationAttribute
{
    /// <inheritdoc />
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateOnly date && date > DateOnly.FromDateTime(DateTime.UtcNow))
            return new ValidationResult(ErrorMessage ?? "Date of birth cannot be in the future.");

        return ValidationResult.Success;
    }
}
