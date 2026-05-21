using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.Groups;

/// <summary>
/// Request body for creating a new group.
/// </summary>
public class CreateGroupRequest
{
    /// <summary>Display name of the group.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Optional description of the group's purpose.</summary>
    [MaxLength(500)]
    public string? Description { get; init; }

    /// <summary>The parent group's identifier. Omit to create a root group.</summary>
    public Guid? ParentGroupId { get; init; }
}