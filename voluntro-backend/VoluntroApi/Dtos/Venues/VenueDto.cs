using VoluntroApi.Dtos.Shared;

namespace VoluntroApi.Dtos.Venues;

/// <summary>
/// A lightweight summary of a venue, suitable for list views.
/// </summary>
public class VenueBriefDto
{
    /// <summary>
    /// Unique identifier of the venue.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Display name of the venue.
    /// </summary>
    public string Name {get; set;} = string.Empty;
    
    /// <summary>
    /// UTC timestamp when the venue was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp when the venue was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Indicates whether the venue has been soft-deleted.
    /// </summary>
    public bool IsDeleted {get; set;}
}

/// <summary>
/// Full details of a venue, including description and address.
/// </summary>
public class VenueDto : VenueBriefDto
{
    /// <summary>
    /// Short description of the venue.
    /// </summary>
    public string Description {get; set;} = string.Empty;

    /// <summary>
    /// Physical address of the venue.
    /// </summary>
    public AddressDto Address { get; init; }
}