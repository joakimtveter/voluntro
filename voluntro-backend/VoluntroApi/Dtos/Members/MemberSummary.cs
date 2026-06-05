using VoluntroApi.Dtos.Tags;

namespace VoluntroApi.Dtos.Members;

/// <summary>
/// Represents a member returned from the API.
/// </summary>
public class MemberSummary
{
    /// <summary>
    /// Unique identifier for the member.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// First name of the member.
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// Middle name(s) of the member, if any.
    /// </summary>
    public string? MiddleNames { get; init; }

    /// <summary>
    /// Last name of the member.
    /// </summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Date of birth of the member, if known.
    /// </summary>
    public DateOnly? DateOfBirth { get; init; }

    /// <summary>
    /// Date of birth of the member, if known.
    /// </summary>
    public List<TagDto> Tags { get; init; } = [];

    /// <summary>
    /// Gender of the member.
    /// </summary>
    public LegalGender LegalGender { get; init; }

    /// <summary>
    /// UTC timestamp when the member was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// UTC timestamp when the member was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Indicates whether the member has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; init; }
    
}