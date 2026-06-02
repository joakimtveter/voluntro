namespace VoluntroApi.Dtos.Groups;

/// <summary>
/// Lightweight group representation used in paginated list responses.
/// </summary>
public class GroupBriefDto
{
    /// <summary>Unique identifier for the group.</summary>
    public Guid Id { get; init; }

    /// <summary>Display name of the group.</summary>
    public string Name { get; init; }

    /// <summary>The identifier of the parent group, or <c>null</c> if this is a root group.</summary>
    public Guid? ParentGroupId { get; init; }

    /// <summary>The date and time the group was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>The date and time the group was last modified (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>When <c>true</c>, the group has been soft-deleted.</summary>
    public bool IsDeleted { get; init; }
}