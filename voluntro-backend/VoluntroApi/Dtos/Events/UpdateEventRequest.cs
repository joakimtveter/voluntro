using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.Events;

/// <summary>
/// Request body for updating an existing event.
/// </summary>
public class UpdateEventRequest
{
    /// <summary>
    /// The event title. Maximum 100 characters.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// A description of the event. Maximum 250 characters.
    /// </summary>
    [Required]
    [MaxLength(250)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The date and time the event starts.
    /// </summary>
    [Required]
    public DateTimeOffset StartsAt { get; set; }

    /// <summary>
    /// The date and time the event ends.
    /// </summary>
    [Required]
    public DateTimeOffset EndsAt { get; set; }
}