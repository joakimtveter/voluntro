using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Models;

/// <summary>
/// Represents a classification type for members (e.g. "Volunteer", "Staff").
/// </summary>
public class MemberType
{
    /// <summary>
    /// Unique identifier for the member type.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Display name of the member type.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// When true, this type is automatically assigned to new members.
    /// Exactly one member type should be the default at any time.
    /// </summary>
    [Required]
    public bool IsDefault { get; set; }
}