using Microsoft.AspNetCore.Mvc;
using VoluntroApi.Dtos.Tags;
using VoluntroApi.Services;

namespace VoluntroApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Consumes("application/json")]
[Produces("application/json", "application/xml")]
public class TagsController(
    ITagService tagService,
    ILogger<MembersController> logger
) : ControllerBase
{
    [HttpGet(Name = "GetTags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TagDto>>> GetTags(CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting list of all tags");

        var tags = await tagService.GetAllAsync(cancellationToken); 
        
        logger.LogDebug("Found {Count} tags", tags.Count);
        
        return Ok(tags);
    }

    [HttpPost(Name = "CreateTag")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TagDto>> CreateTag([FromBody] CreateTagRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating tag with name {TagName}", request.Name);
        
        var tag = await tagService.CreateAsync(request, cancellationToken);
        
        logger.LogInformation("Tag with name {TagName} created with TagId={TagId}", request.Name, tag.Id);

        return Ok(tag);
    }
    
    [HttpPut("{tagId:guid}", Name = "UpdateTag")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagDto>> UpdateTag([FromRoute] Guid tagId, [FromBody] UpdateTagRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Updating tag with TagId={TagId}", tagId);
        
        var tag = await tagService.UpdateAsync(tagId, request, cancellationToken);
        
        if (tag is null)
        {
            logger.LogWarning("Could not update tag. Tag with TagId={TagId} not found.", tagId);
            return NotFound("Tag not found.");
        }
        
        logger.LogDebug("Tag with TagId={TagId} updated.", tagId);
        
        return Ok(tag);
    }
    
    [HttpDelete("{tagId:guid}", Name = "DeleteTag")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteTag([FromRoute] Guid tagId, CancellationToken cancellationToken)
    {
        logger.LogDebug("Deleting tag with TagId={TagId}", tagId);
        
        var deleted = await tagService.DeleteAsync(tagId, cancellationToken);
        
        if (deleted is null)
        {
            logger.LogWarning("Could not delete tag. Tag with TagId={TagId} not found.", tagId);
            return NotFound("Tag not found.");
        }
        
        logger.LogInformation("Tag with TagId={TagId} deleted.", tagId);
        
        return NoContent();
    }
}