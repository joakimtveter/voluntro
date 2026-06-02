using VoluntroApi.Dtos.Shared;

namespace VoluntroApi.Services;

/// <summary>
/// Service for fetching lightweight option lists used in search and select inputs.
/// </summary>
public interface IQueryService
{
    /// <summary>
    /// Returns a list of venues matching the search query, for use in select inputs.
    /// </summary>
    /// <param name="query">Search term to filter venues by name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<List<SelectOptionDto>> GetVenueOptions(string? query, CancellationToken cancellationToken);
   
    /// <summary>
    /// Returns a list of groups matching the search query, for use in select inputs.
    /// </summary>
    /// <param name="query">Search term to filter groups by name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<List<SelectOptionDto>> GetGroupOptions(string? query, CancellationToken cancellationToken);
    
    /// <summary>
    /// Returns a list of members matching the search query, for use in select inputs.
    /// </summary>
    /// <param name="query">Search term to filter members by name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<List<SelectOptionDto>> GetMemberOptions(string? query, CancellationToken cancellationToken);
}