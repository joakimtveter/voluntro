using Microsoft.AspNetCore.Mvc;
using VoluntroApi.Dtos.MemberTypes;
using VoluntroApi.Services;

namespace VoluntroApi.Controllers;

[ApiController]
[Route("api/admin/MemberTypes")]
[Produces("application/json", "application/xml")]
[Consumes("application/json")]
public class MemberTypesAdminController(IMemberTypeService memberTypeService,  ILogger<MemberTypesAdminController> logger) : ControllerBase
{
    [HttpGet(Name = "GetMemberTypes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MemberTypeDto>>> GetMemberTypes(CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching all MemberTypes.");

        var results = await memberTypeService.GetAllAsync(cancellationToken);
        
        logger.LogDebug("Found {Count} MemberTypes.", results.Count);
        
        return Ok(results);
    }
    
    [HttpPost(Name = "CreateMemberType")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MemberTypeDto>> CreateMemberType([FromBody] CreateMemberTypeRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating MemberType with name {Name}.", request.Name);
        
        var memberType = await memberTypeService.CreateAsync(request, cancellationToken);
        
        logger.LogInformation("MemberType {Name} created MemberTypeId={MemberTypeId}.", memberType.Name, memberType.Id);
        
        return CreatedAtAction(nameof(GetMemberTypes), new { memberTypeId = memberType.Id }, memberType);
        
        
    }

    [HttpPut("{memberTypeId:guid}", Name = "UpdateMemberType")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberTypeDto>> UpdateMemberType(
        [FromRoute] Guid memberTypeId, 
        [FromBody]UpdateMemberTypeRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Updating MemberType {ID} with name {Name}.",memberTypeId, request.Name);

        var result = await memberTypeService.UpdateAsync(memberTypeId, request, cancellationToken);

        if (result is null)
        {
            logger.LogWarning("Member type not found MemberTypeId={MemberTypeId}", memberTypeId);
            return NotFound("Member type not found.");
        }
        
        return Ok(result);
    }
    
    [HttpPut("{id:guid}/set-default", Name = "SetDefaultMemberType")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberTypeDto>> SetDefaultMemberType(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await memberTypeService.SetDefaultAsync(id, cancellationToken);

        if (result is null)
        {
            logger.LogWarning("Member type not found MemberTypeId={MemberTypeId}", id);
            return NotFound("Member type not found.");
        }

        return Ok(result);
    }
    
    [HttpDelete("{id:guid}", Name = "DeleteMemberType")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> DeleteMemberType(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await memberTypeService.DeleteAsync(id, cancellationToken);

        return result switch
        {
            DeleteMemberTypeResult.Success => NoContent(),
            DeleteMemberTypeResult.NotFound => NotFound("Member type not found."),
            DeleteMemberTypeResult.HasMembers => Conflict("Member type that has members assigned to it."),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
    
}