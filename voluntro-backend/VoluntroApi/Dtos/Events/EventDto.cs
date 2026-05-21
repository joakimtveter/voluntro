using VoluntroApi.Dtos.Shared;

namespace VoluntroApi.Dtos.Events;

/// <summary>
/// A lightweight summary of an event, suitable for list views.
/// </summary>
public class EventBriefDto
{
    /// <summary>
    /// Unique identifier of the event.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Display title of the event.
    /// </summary>
    public string Title {get; set;} = string.Empty;

    /// <summary>
    /// Short description or summary of the event.
    /// </summary>
    public string Description {get; set;} = string.Empty;

    /// <summary>
    /// Date and time when the event starts, including timezone offset.
    /// </summary>
    public DateTimeOffset StartsAt {get; set;}

    /// <summary>
    /// Date and time when the event ends, including timezone offset.
    /// </summary>
    public DateTimeOffset EndsAt {get; set;}

    /// <summary>
    /// Name of the venue where the event takes place.
    /// </summary>
    public string Venue {get; set;} = string.Empty;
}

/// <summary>
/// Full details of an event, including venue address.
/// </summary>
public class EventDto : EventBriefDto
{
    /// <summary>
    /// Physical address of the venue.
    /// </summary>
    public AddressDto Address {get; set;}
}