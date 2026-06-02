using Microsoft.AspNetCore.Mvc;
using VoluntroApi.Dtos.Groups;
using VoluntroApi.Services;

namespace VoluntroApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Consumes("application/json")]
[Produces("application/json", "application/xml")]
public class GroupsController(IGroupService groupService, ILogger<GroupsController> logger) : ControllerBase
{
    /// <summary>Returns a paginated list of groups.</summary>
    [HttpGet(Name = "GetGroups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GroupBriefDto>> GetGroups(
        [FromQuery] GetGroupsQuery query, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Retrieving groups with ParentGroupId: {ParentGroupId}, include non root page. Page {Page}, pageSize {PageSize}",
            query.ParentGroupId, query.Page, query.PageSize);

        var groups = await groupService.GetAllAsync(query, cancellationToken, includeDeleted: false);

        logger.LogDebug("Found {Count} groups. Returned page {PageNumber} of {TotalPages}", groups.TotalCount,
            groups.CurrentPage, groups.TotalPages);


        return Ok(groups);
    }

    /// <summary>Returns a single group by its ID.</summary>
    /// <param name="groupId">The group's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The group, or 404 if not found.</returns>
    [HttpGet("{groupId:guid}", Name = "GetGroupById")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDto>> GetGroupById([FromRoute] Guid groupId,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching group with Id={groupId}.", groupId);

        var group = await groupService.GetByIdAsync(groupId, cancellationToken, includeDeleted: false);

        if (group is null)
        {
            logger.LogWarning("Group not found with GroupId={groupId}", groupId);
            return NotFound();
        }

        return Ok(group);
    }

    /// <summary>Creates a new group.</summary>
    [HttpPost(Name = "CreateGroup")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GroupDto>> CreateGroup(
        [FromBody] CreateGroupRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating group {GroupName}", request.Name);

        var group = await groupService.CreateAsync(request, cancellationToken);

        if (group is null)
        {
            logger.LogWarning("Failed to create group with parent group {ParentGroupId}", request.ParentGroupId);
            return NotFound();
        }

        logger.LogDebug("Group with Id {GroupId} created", group.Id);

        return CreatedAtAction(nameof(GetGroupById), new { groupId = group.Id }, group);
    }
    
    /// <summary>Updates a new group.</summary>
    /// <param name="groupId">The group's unique identifier.</param>
    /// <param name="request">The group details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    [HttpPut("{groupId:guid}", Name = "UpdateGroup")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GroupDto>> UpdateGroup(
        [FromRoute] Guid groupId, [FromBody] UpdateGroupRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Updating group with id:  {GroupName}", request.Name);

        var (group, result) = await groupService.UpdateAsync(groupId, request, cancellationToken);

        if (result == UpdateGroupResult.NotFound)
        {
            logger.LogWarning("Failed to update group with id {groupId} - Group not found", groupId);
            return NotFound("Group not found");
        }
        
        if (result == UpdateGroupResult.CycleDetected)
        {
            logger.LogWarning("Failed to update group with parent group {ParentGroupId} - Cycle detected", request.ParentGroupId);
            return Conflict("Cycle detected in group hierarchy");
        }
        
        if (result == UpdateGroupResult.ParentNotFound)
        {
            logger.LogWarning("Failed to update group with parent group {ParentGroupId} - Parent group not found", request.ParentGroupId);
            return NotFound("Parent group not found");
        }

        if (result == UpdateGroupResult.Success && group is null)
        {
            logger.LogError("Failed to update group with id {groupId} - service returned null without UpdateGroupResult", groupId);
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Internal Server Error",
                detail: "Failed to update group");
        }
        
        if (group is null) throw new Exception("Unexpected result from group service");

        logger.LogDebug("Group with Id {GroupId} updated", group.Id);

        return Ok(group);
    }

    /// <summary>
    /// Deletes a group by its unique identifier.
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="cancellationToken"></param>
    [HttpDelete("{groupId:guid}",Name = "DeleteGroup")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteGroup([FromRoute] Guid groupId, CancellationToken cancellationToken)
    {
        logger.LogDebug("Deleting group with Id {GroupId}", groupId);

        var result = await groupService.DeleteAsync(groupId, cancellationToken);

        if (result == DeleteGroupResult.NotFound)
        {
            logger.LogWarning("Group with Id {GroupId} not found", groupId);
            return NotFound();
        }

        if (result == DeleteGroupResult.HasChildren)
        {
            logger.LogWarning("Group with Id {GroupId} has children and cannot be deleted", groupId);
            return Conflict("Group has children and cannot be deleted");
        }
        
        return result == DeleteGroupResult.Success ? NoContent() : throw new Exception("Unexpected result from group service");
    }
}