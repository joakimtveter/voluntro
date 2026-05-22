using Microsoft.AspNetCore.Mvc;
using VoluntroApi.Dtos.Shared;
using VoluntroApi.Services;

namespace VoluntroApi.Controllers;

/// <summary>
/// Returns options for combo boxes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json", "application/xml")]
public class QueryController(IQueryService queryService,  ILogger<QueryController> logger): ControllerBase 
{
    /// <summary>
    /// Returns a list of venues matching the search term, for use in select inputs.
    /// </summary>
    /// <param name="query">Search term to filter venues by name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching venue options.</returns>
    [HttpGet("Venues")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SelectOptionDto>>> GetVenueOptions(
        [FromQuery] string? query, 
        CancellationToken cancellationToken)
    {
        var options = await queryService.GetVenueOptions(query, cancellationToken);
        return Ok(options);
    }    
    
    /// <summary>
    /// Returns a list of groups matching the search term, for use in select inputs.
    /// </summary>
    /// <param name="query">Search term to filter groups by name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching group options.</returns>
    [HttpGet("Groups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SelectOptionDto>>> GetGroupOptions(
        [FromQuery] string? query, 
        CancellationToken cancellationToken)
    {
        var options = await queryService.GetGroupOptions(query, cancellationToken);
        return Ok(options);
    }
    
    /// <summary>
    /// Returns a list of members matching the search term, for use in select inputs.
    /// </summary>
    /// <param name="query">Search term to filter groups by name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching group options.</returns>
    [HttpGet("Members")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SelectOptionDto>>> GetMemberOptions(
        [FromQuery] string? query, 
        CancellationToken cancellationToken)
    {
        var options = await queryService.GetMemberOptions(query, cancellationToken);
        return Ok(options);
    }   
}