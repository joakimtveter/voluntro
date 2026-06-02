using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.Members;

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