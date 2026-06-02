using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Models;

/// <summary>
/// Represents a group within the organisation, such as a sports team, age category, or ministry team.
/// Groups are arranged in a hierarchy via <see cref="ParentGroupId"/>. A member who joins a child group
/// is automatically enrolled in all ancestor groups.
/// </summary>
public class Group
{
    /// <summary>Unique identifier for the group.</summary>
    public Guid Id { get; init; }

    /// <summary>Display name of the group.</summary>
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>Optional description providing more detail about the group's purpose.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// The identifier of this group's parent, or <c>null</c> if this is a root group.
    /// </summary>
    public Guid? ParentGroupId { get; set; }

    /// <summary>The date and time the group was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>The date and time the group was last modified (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Navigation property to the parent group.</summary>
    public Group? ParentGroup { get; set; }

    /// <summary>Direct child groups nested under this group.</summary>
    public ICollection<Group> ChildGroups { get; set; } = [];

    /// <summary>Join records linking members to this group.</summary>
    public ICollection<MemberGroup> MemberGroups { get; set; } = [];

    /// <summary>When <c>true</c>, the group has been soft-deleted and is no longer active.</summary>
    [Required]
    public bool IsDeleted { get; set; }
}