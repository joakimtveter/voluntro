using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.Groups;

/// <summary>
/// Query parameters for retrieving a paginated list of groups.
/// </summary>
public class GetGroupsQuery
{
    /// <summary>Page number to retrieve (1-based). Defaults to 1.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    /// <summary>Number of groups per page (1–100). Defaults to 25.</summary>
    [Range(1, 100)]
    public int PageSize { get; init; } = 25;

    /// <summary>
    /// When set, returns only the direct children of this group.
    /// </summary>
    public Guid? ParentGroupId { get; init; }
}

/// <summary>
/// Query parameters for retrieving a paginated list of groups when logged in as an admin.
/// </summary>
public class AdminGetGroupsQuery : GetGroupsQuery
{
    /// <summary>
    /// Include deleted groups in the result.
    /// </summary>
    public bool IncludeDeleted { get; init; } = false;
}