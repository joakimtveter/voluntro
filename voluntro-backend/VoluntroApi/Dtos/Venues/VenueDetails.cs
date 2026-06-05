using VoluntroApi.Dtos.Shared;

namespace VoluntroApi.Dtos.Venues;

/// <summary>
/// Full details of a venue, including description and address.
/// </summary>
public class VenueDetails : VenueSummary
{
    /// <summary>
    /// Short description of the venue.
    /// </summary>
    public string Description {get; set;} = string.Empty;

    /// <summary>
    /// Physical address of the venue.
    /// </summary>
    public Address Address { get; init; } = new Address();
    
    /// <summary>
    /// UTC timestamp when the venue was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp when the venue was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}