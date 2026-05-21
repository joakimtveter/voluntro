using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.Events;

/// <summary>
/// Request body for creating a new event.
/// </summary>
public class CreateEventRequest
{
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
    /// Identifier of the venue where the event will take place.
    /// </summary>
    [Required]
    public Guid VenueId {get; set;}
}