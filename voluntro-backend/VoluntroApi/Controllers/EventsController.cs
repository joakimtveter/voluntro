using Microsoft.AspNetCore.Mvc;
using VoluntroApi.Dtos.Events;
using VoluntroApi.Dtos.Shared;
using VoluntroApi.Services;

namespace VoluntroApi.Controllers;

/// <summary>
/// Events are happenings that take place in your church or are connected to it
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json", "application/xml")]
[Consumes("application/json")]
public class EventsController(IEventService eventService, ILogger<EventsController> logger) : ControllerBase
{
    /// <summary>
    /// Returns a paginated list of events, optionally filtered by date range.
    /// </summary>
    /// <param name="query">Pagination and date filter parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result of events.</returns>
    [HttpGet(Name = "GetEvents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetEvents(
        [FromQuery] GetEventsQuery query, 
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching events on Page={Page} with PageSize={PageSize}", query.Page, query.PageSize);
        
        var events = await eventService.GetAllAsync(query, cancellationToken);
        
        logger.LogDebug("Found {Count} events. Returned page {PageNumber} of {TotalPages}", events.TotalCount, events.CurrentPage, events.TotalPages);

        return Ok(events);
    }

    /// <summary>
    /// Returns a single event by its unique identifier.
    /// </summary>
    /// <param name="eventId">The event's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The event, or 404 if not found.</returns>
    [HttpGet("{eventId:guid}", Name = "GetEventById")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetEventById(
        [FromRoute] Guid eventId, 
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching Event with EventId={eventId}", eventId);

        var singleEvent = await eventService.GetByIdAsync(eventId, cancellationToken);

        if (singleEvent is null)
        {
            logger.LogWarning("Event not found with EventId={eventId}", eventId);
            return NotFound();
        }

        return Ok(singleEvent);
    }

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="request">The event details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created event.</returns>
    [HttpPost(Name = "CreateEvent")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventDto>> CreateEvent(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating event");
        var newEvent = await eventService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetEventById), new { eventId = newEvent.Id }, newEvent);
    }

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="eventId">The event's unique identifier.</param>
    /// <param name="request">The updated event details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated event, or 404 if not found.</returns>
    [HttpPut("{eventId:guid}", Name = "UpdateEvent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> UpdateEvent(
        [FromRoute] Guid eventId,
        [FromBody] UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Updating event with EventId={EventId}", eventId);

        var updatedEvent = await eventService.UpdateAsync(eventId, request, cancellationToken);

        if (updatedEvent is null)
        {
            logger.LogWarning("Event not found with EventId={EventId}", eventId);
            return NotFound();
        }

        return Ok(updatedEvent);
    }

    /// <summary>
    /// Deletes an event by its unique identifier.
    /// </summary>
    /// <param name="eventId">The event's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content if deleted, 404 if not found.</returns>
    [HttpDelete("{eventId:guid}", Name = "DeleteEvent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteEvent([FromRoute] Guid eventId, CancellationToken cancellationToken)
    {
        logger.LogDebug("Deleting event with EventId={EventId}", eventId);
        
        var deleted = await eventService.DeleteAsync(eventId, cancellationToken);
        
        if (!deleted)
        {
            logger.LogWarning("Could not delete event. Event with EventId={eventId} not found.", eventId);
            return NotFound();
        }
        return NoContent();
    }
    
}
