using Microsoft.AspNetCore.Mvc;
using VoluntroApi.Dtos.Groups;
using VoluntroApi.Services;

namespace VoluntroApi.Controllers;

[ApiController]
[Route("api/admin/groups")]
[Produces("application/json", "application/xml")]
[Consumes("application/json")]
public class AdminGroupsController(
    IGroupService groupService,
    ILogger<AdminMembersController> logger
) : ControllerBase
{
    /// <summary>Returns a paginated list of groups.</summary>
    [HttpGet(Name = "AdminGetGroups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GroupBriefDto>> AdminGetGroups(
        [FromQuery] AdminGetGroupsQuery query, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Admin: Retrieving groups with ParentGroupId: {ParentGroupId}, include non root page. Page {Page}, pageSize {PageSize}",
            query.ParentGroupId, query.Page, query.PageSize);

        var groups = await groupService.GetAllAsync(query, cancellationToken, includeDeleted: query.IncludeDeleted);

        logger.LogDebug("Found {Count} groups. Returned page {PageNumber} of {TotalPages}", groups.TotalCount,
            groups.CurrentPage, groups.TotalPages);


        return Ok(groups);
    }
    
    /// <summary>Returns a single group by its ID.</summary>
    /// <param name="groupId">The group's unique identifier.</param>
    /// <param name="includeDeleted">If the inc</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The group, or 404 if not found.</returns>
    [HttpGet("{groupId:guid}", Name = "AdminGetGroupById")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDto>> GetGroupById([FromRoute] Guid groupId, [FromQuery] bool includeDeleted,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("ADMIN: Fetching group with Id={GroupId}, includeDeleted={includeDeleted}.", groupId,  includeDeleted);

        var group = await groupService.GetByIdAsync(groupId, cancellationToken, includeDeleted);

        if (group is null)
        {
            logger.LogWarning("Group not found with GroupId={groupId}", groupId);
            return NotFound();
        }

        return Ok(group);
    }
    
    /// <summary>Returns a single group by its ID.</summary>
    /// <param name="groupId">The group's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The group, or 404 if not found.</returns>
    [HttpPost("{groupId:guid}/Restore", Name = "AdminDeleteGroup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDto>> RestoreGroup([FromRoute] Guid groupId,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("ADMIN: Restore group with Id={GroupId}.", groupId);

        var (group, groupResult) = await groupService.RestoreAsync(groupId, cancellationToken);

        return groupResult switch
        {
            RestoreGroupResult.Success => Ok(group),
            RestoreGroupResult.NotFound => NotFound(),
            RestoreGroupResult.AlreadyRestored => Conflict("Group not deleted"),
            RestoreGroupResult.ParentDoesNotExist => Conflict("Parent group not found"),
            _ => throw new Exception("Unexpected result from group service")
        };
    }
}