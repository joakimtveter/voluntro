using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.Members;

public enum MemberSortBy { LastName, FirstName, DateOfBirth }
public enum SortDirection { Asc, Desc }
public enum TagFilterMode { Any, All }

/// <summary>
/// Query parameters for retrieving a paginated list of members.
/// </summary>
public class GetMembersQuery
{
    /// <summary>
    /// Page number to retrieve (1-based). Defaults to 1.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of members per page (1–100). Defaults to 15.
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; init; } = 15;

    /// <summary>
    /// Filter to members that have at least one of the specified tags. Omit or leave empty to return all members.
    /// </summary>
    public List<Guid>? TagIds { get; init; }

    /// <summary>
    /// Whether a member must have ANY (default) or ALL of the specified tags to match.
    /// </summary>
    public TagFilterMode TagFilterMode { get; init; } = TagFilterMode.Any;

    /// <summary>
    /// Filter to members with the specified legal gender. Omit to return all members.
    /// </summary>
    public LegalGender? LegalGender { get; init; }

    public MemberSortBy SortBy { get; init; } = MemberSortBy.LastName;
    public SortDirection SortOrder { get; init; } = SortDirection.Asc;
}

/// <summary>
/// Query parameters for retrieving a paginated list of members when logged in as an admin.
/// </summary>
public class AdminGetMembersQuery : GetMembersQuery
{
    /// <summary>
    /// Include deleted members in the result.
    /// </summary>
    public bool IncludeDeleted { get; init; } = false;

}