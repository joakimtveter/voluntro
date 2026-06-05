using VoluntroApi.Dtos.Shared;
using VoluntroApi.Dtos.Venues;

namespace VoluntroApi.Dtos.Events;

/// <summary>
/// Full details of an event, including venue address.
/// </summary>
public class EventDetails : EventSummary
{
    /// <summary>
    /// Physical address of the venue.
    /// </summary>
    public VenueDetails Venue {get; set;} = new VenueDetails();
}