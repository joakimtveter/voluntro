using Microsoft.AspNetCore.Mvc;
using VoluntroApi.Dtos.Shared;
using VoluntroApi.Dtos.Venues;
using VoluntroApi.Services;

namespace VoluntroApi.Controllers;

/// <summary>Venues are locations where events takes place</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json", "application/xml")]
[Consumes("application/json")]
public class VenuesController(IVenueService venueService, ILogger<VenuesController> logger) : ControllerBase
{
    /// <summary>Returns a paginated list of venues.</summary>
    [HttpGet(Name = "GetVenues")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<VenueBriefDto>>> GetVenues(
        [FromQuery] GetVenuesQuery query,  CancellationToken cancellationToken)
    {
        logger.LogDebug("GetVenues");

        var venues = await venueService.GetAllAsync(query, cancellationToken);
        
        logger.LogDebug("Found {Count} venues. Returned page {PageNumber} of {TotalPages}", venues.TotalCount, venues.CurrentPage, venues.TotalPages);

        return Ok(venues);
    }

    /// <summary>Returns a single venue by its ID.</summary>
    [HttpGet("{venueId:guid}", Name = "GetVenueById")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VenueDto>> GetVenueById(
        [FromRoute] Guid venueId, 
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching venue with Id={venueId}.", venueId);

        var venue = await venueService.GetByIdAsync(venueId, cancellationToken,  includeDeleted: false);
        
        if  (venue is null)
        {
            logger.LogWarning("Venue not found with VenueId={venueId}", venueId);
            return NotFound();
        }
        
        return Ok(venue);
    }

    /// <summary>Creates a new venue.</summary>
    [HttpPost(Name = "CreateVenue")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VenueDto>> CreateVenue(
        [FromBody] CreateVenueRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating venue");
        var newVenue = await venueService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetVenueById), new { venueId = newVenue.Id }, newVenue);
    }


    /// <summary>Updates an existing venue.</summary>
    [HttpPut("{venueId:guid}", Name = "UpdateVenue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VenueDto>> UpdateVenue(
        [FromRoute] Guid venueId,
        [FromBody] UpdateVenueRequest request,
        CancellationToken cancellationToken)
    {
        var updatedVenue = await venueService.UpdateAsync(venueId, request, cancellationToken);

        if (updatedVenue is null)
        {
            logger.LogWarning("Venue not found with VenueId={venueId}", venueId);
            return NotFound();
        }
        
        return Ok(updatedVenue);
    }

    /// <summary>Soft-deletes a venue.</summary>
    /// <param name="venueId">The venue's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>204 if deleted, 404 if not found.</returns>
    [HttpDelete("{venueId:guid}", Name = "DeleteVenue")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteVenue([FromRoute] Guid venueId, CancellationToken cancellationToken)
    {
        logger.LogDebug("Deleting venue with Id={VenueId}", venueId);
        
        var deleted = await venueService.DeleteAsync(venueId, cancellationToken);
        
        if (!deleted)
        {
            logger.LogWarning("Could not delete venue. Venue with Id={venueId} not found.", venueId);
            return NotFound();
        }
        return NoContent();
    }    
}