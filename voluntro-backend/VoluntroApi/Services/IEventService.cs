using VoluntroApi.Dtos.Events;
using VoluntroApi.Dtos.Shared;

namespace VoluntroApi.Services;

/// <summary>
/// Defines the contract for event management operations.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Returns a paginated list of events, optionally filtered by date range.
    /// </summary>
    /// <param name="query">Pagination and filter parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result of events.</returns>
    Task<PagedResult<EventDto>> GetAllAsync(GetEventsQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a single event by its unique identifier.
    /// </summary>
    /// <param name="eventId">The event's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The event, or null if not found.</returns>
    Task<EventDto?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="request">The event details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created event.</returns>
    Task<EventDto> CreateAsync(CreateEventRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="eventId">The event's unique identifier.</param>
    /// <param name="request">The updated event details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated event.</returns>
    Task<EventDto?> UpdateAsync(Guid eventId, UpdateEventRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes an event by its unique identifier.
    /// </summary>
    /// <param name="eventId">The event's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the event was deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid eventId, CancellationToken cancellationToken);
}