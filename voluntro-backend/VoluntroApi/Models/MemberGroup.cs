namespace VoluntroApi.Models;

/// <summary>
/// Join entity representing a member's membership in a group.
/// A row is inserted for each group a member belongs to, including ancestor groups
/// that were auto-enrolled when joining a child group.
/// </summary>
public class MemberGroup
{
    /// <summary>The identifier of the member.</summary>
    public Guid MemberId { get; init; }

    /// <summary>The identifier of the group.</summary>
    public Guid GroupId { get; init; }

    /// <summary>The date and time the member was enrolled in this group (UTC).</summary>
    public DateTimeOffset JoinedAt { get; init; }

    /// <summary>Navigation property to the member.</summary>
    public Member Member { get; set; } = null!;

    /// <summary>Navigation property to the group.</summary>
    public Group Group { get; set; } = null!;
}