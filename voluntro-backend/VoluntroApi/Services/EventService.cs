using Microsoft.EntityFrameworkCore;
using VoluntroApi.Data;
using VoluntroApi.Dtos.Events;
using VoluntroApi.Dtos.Shared;
using VoluntroApi.Models;

namespace VoluntroApi.Services;

/// <summary>
/// EF Core-backed implementation of <see cref="IEventService"/>.
/// </summary>
public class EventService(AppDbContext db, ILogger<EventService> logger) : IEventService
{
    /// <inheritdoc />
    public async Task<PagedResult<EventDto>> GetAllAsync(GetEventsQuery query, CancellationToken cancellationToken)
    {
        logger.LogDebug("Querying events Page={Page} PageSize={PageSize}", query.Page, query.PageSize);
        
        var from = new DateTimeOffset(
            query.From.ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        var to = query.To.HasValue
            ? new DateTimeOffset(query.To.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero)
            : (DateTimeOffset?)null;

        return await db.Events
            .AsNoTracking()
            .Where(e => e.IsDeleted == false)
            .Where(e => e.StartsAt >= from)
            .Where(e => to == null || e.StartsAt <= to)
            .OrderBy(e => e.StartsAt)
            .ToPagedResultAsync( e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                StartsAt = e.StartsAt,
                EndsAt = e.EndsAt,
            }, query.Page, query.PageSize, cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task<EventDto?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        logger.LogDebug("Querying event with EventId={EventId}", eventId);

        return await db.Events
            .AsNoTracking()
            .Where(e => e.Id == eventId && e.IsDeleted == false)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                StartsAt = e.StartsAt,
                EndsAt = e.EndsAt,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EventDto> CreateAsync(CreateEventRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Creating event with Title={Title}", request.Title);

        var newEvent = new Event
        {
            Title = request.Title,
            Description = request.Description,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
        };

        db.Events.Add(newEvent);
        await db.SaveChangesAsync(cancellationToken);

        return new EventDto
        {
            Id = newEvent.Id,
            Title = newEvent.Title,
            Description = newEvent.Description,
            StartsAt = newEvent.StartsAt,
            EndsAt = newEvent.EndsAt,
        };
    }
    
    /// <inheritdoc />
    public async Task<EventDto?> UpdateAsync(Guid eventId, UpdateEventRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Updating event with EventId={EventId}", eventId);

        var eventToUpdate = await db.Events.FindAsync([eventId], cancellationToken);

        if (eventToUpdate is null || eventToUpdate.IsDeleted)
        {
            logger.LogWarning("Update failed — event not found EventId={EventId}", eventId);
            return null;
        }

        eventToUpdate.Title = request.Title;
        eventToUpdate.Description = request.Description;
        eventToUpdate.StartsAt = request.StartsAt;
        eventToUpdate.EndsAt = request.EndsAt;

        await db.SaveChangesAsync(cancellationToken);

        return new EventDto
        {
            Id = eventToUpdate.Id,
            Title = eventToUpdate.Title,
            Description = eventToUpdate.Description,
            StartsAt = eventToUpdate.StartsAt,
            EndsAt = eventToUpdate.EndsAt,
        };
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var eventToDelete = await db.Events.FindAsync([eventId], cancellationToken);

        if (eventToDelete is null || eventToDelete.IsDeleted)
        {
            logger.LogWarning("Delete failed — event not found EventId={EventId}", eventId);
            return false;
        }

        eventToDelete.IsDeleted = true;
        eventToDelete.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}