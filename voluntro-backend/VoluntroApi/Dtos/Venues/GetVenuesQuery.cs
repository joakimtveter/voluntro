using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.Venues;

/// <summary>
/// Query parameters for retrieving a paginated list of venues.
/// </summary>
public class GetVenuesQuery
{
    /// <summary>
    /// 1-based page number to retrieve. Defaults to 1.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of venues per page. Defaults to 100, maximum 200.
    /// </summary>
    [Range(1, 200)]
    public int PageSize { get; set; } = 100;
}