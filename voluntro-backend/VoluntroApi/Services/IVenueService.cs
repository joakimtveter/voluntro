using VoluntroApi.Dtos.Shared;
using VoluntroApi.Dtos.Venues;

namespace VoluntroApi.Services;

/// <summary>
/// Service for managing venues.
/// </summary>
public interface IVenueService
{
    /// <summary>
    /// Returns a paginated list of active venues.
    /// </summary>
    Task<PagedResult<VenueBriefDto>> GetAllAsync(GetVenuesQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a venue by its unique identifier, or <c>null</c> if not found.
    /// </summary>
    Task<VenueDto?> GetByIdAsync(Guid venueId, CancellationToken cancellationToken, bool includeDeleted);

    /// <summary>
    /// Creates a new venue and returns the created record.
    /// </summary>
    Task<VenueDto> CreateAsync(CreateVenueRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing venue and returns the updated record, or <c>null</c> if not found.
    /// </summary>
    Task<VenueDto?> UpdateAsync(Guid venueId, UpdateVenueRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes a venue. Returns <c>true</c> if deleted, <c>false</c> if not found.
    /// </summary>
    Task<bool> DeleteAsync(Guid venueId, CancellationToken cancellationToken);
    
}