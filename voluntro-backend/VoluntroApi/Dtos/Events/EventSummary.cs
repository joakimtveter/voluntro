namespace VoluntroApi.Dtos.Events;

/// <summary>
/// A lightweight summary of an event, suitable for list views.
/// </summary>
public class EventSummary
{
    /// <summary>
    /// Unique identifier of the event.
    /// </summary>
    public Guid Id { get; init; }

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
    public string VenueName {get; set;} = string.Empty;
}