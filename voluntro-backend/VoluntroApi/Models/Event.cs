using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Models;

/// <summary>
/// Represents a church event.
/// </summary>
public class Event
{
    /// <summary>
    /// Unique identifier of the event.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Display title of the event. Maximum 100 characters.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Title {get; set;} = string.Empty;

    /// <summary>
    /// Short description or summary of the event. Maximum 250 characters.
    /// </summary>
    [Required]
    [MaxLength(250)]
    public string Description {get; set;} = string.Empty;

    /// <summary>
    /// Date and time when the event starts, including timezone offset.
    /// </summary>
    [Required]
    public DateTimeOffset StartsAt {get; set;}

    /// <summary>
    /// Date and time when the event ends, including timezone offset.
    /// </summary>
    [Required]
    public DateTimeOffset EndsAt {get; set;}

    /// <summary>
    /// Foreign key referencing the venue where the event takes place.
    /// </summary>
    [Required]
    public Guid VenueId {get; set;}

    /// <summary>
    /// Navigation property to the venue where the event takes place.
    /// </summary>
    [Required]
    public Venue Venue {get; set;}
    
    /// <summary>
    /// UTC timestamp when the event was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// UTC timestamp when the event was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
    
    /// <summary>
    /// Indicates whether the event has been soft-deleted.
    /// </summary>
    [Required]
    public bool IsDeleted {get; set;}
}