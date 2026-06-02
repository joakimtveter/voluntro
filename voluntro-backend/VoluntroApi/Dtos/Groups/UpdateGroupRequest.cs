using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.Groups;

/// <summary>
/// Request body for updating an existing group.
/// </summary>
public class UpdateGroupRequest
{
    /// <summary>Updated display name of the group.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Updated description, or <c>null</c> to clear it.</summary>
    [MaxLength(500)]
    public string? Description { get; init; }

    /// <summary>
    /// Updated parent group identifier. Pass <c>null</c> to make this a root group.
    /// Setting this to a descendant of the group will be rejected to prevent cycles.
    /// </summary>
    public Guid? ParentGroupId { get; init; }
}