using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.Shared;

/// <summary>
/// A generic key-label pair for use in dropdown and select inputs.
/// </summary>
public class SelectOptionDto
{
    /// <summary>
    /// Unique identifier of the option.
    /// </summary>
    [Required]
    public Guid Id { get; init; }

    /// <summary>
    /// Human-readable label displayed to the user.
    /// </summary>
    [Required]
    public string Label { get; init; } = string.Empty;
}