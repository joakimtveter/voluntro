using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.Events;

/// <summary>
/// Query parameters for filtering and paginating events.
/// </summary>
public class GetEventsQuery
{
    /// <summary>
    /// Page number to retrieve (1-based). Defaults to 1.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of events per page (1–100). Defaults to 15.
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 15;

    /// <summary>
    /// Only return events starting on or after this date. Defaults to today.
    /// </summary>
    public DateOnly From { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>
    /// Only return events starting on or before this date. Optional.
    /// </summary>
    public DateOnly? To { get; set; }
}