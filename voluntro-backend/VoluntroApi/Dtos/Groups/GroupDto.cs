namespace VoluntroApi.Dtos.Groups;

/// <summary>
/// Full group representation returned from single-item endpoints.
/// Extends <see cref="GroupBriefDto"/> with description, direct children, and member count.
/// </summary>
public class GroupDto : GroupBriefDto
{
    /// <summary>Optional description of the group's purpose.</summary>
    public string? Description { get; init; }

    /// <summary>The name of the parent group, or <c>null</c> if this is a root group.</summary>
    public string? ParentGroupName { get; init; }

    /// <summary>Direct child groups nested under this group (excludes soft-deleted children).</summary>
    public IReadOnlyList<GroupBriefDto> ChildGroups { get; init; } = [];

    /// <summary>Number of active (non-deleted) members currently in this group.</summary>
    public int MemberCount { get; init; }
}