using Microsoft.AspNetCore.Mvc;
using VoluntroApi.Dtos.GroupMemberships;
using VoluntroApi.Dtos.Groups;
using VoluntroApi.Dtos.Members;
using VoluntroApi.Services;

namespace VoluntroApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Consumes("application/json")]
[Produces("application/json", "application/xml")]
public class GroupMembershipsController(IGroupMembershipService groupMembershipService, ILogger<GroupsController> logger) : ControllerBase
{
    /// <summary>Returns a paginated list of members belonging to a group.</summary>
    /// <param name="groupId">The group's unique identifier.</param>
    /// <param name="query">Pagination parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    [HttpGet("Group/{groupId:guid}", Name = "GetGroupMembers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberSummary>> GetGroupMembers(
        [FromRoute] Guid groupId, [FromQuery] GetMembersQuery query, CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching members for GroupId={GroupId}", groupId);

        var members = await groupMembershipService.GetMembersAsync(groupId, query, cancellationToken);

        if (members is null)
        {
            logger.LogWarning("Group not found with GroupId={GroupId}", groupId);
            return NotFound();
        }

        return Ok(members);
    }    
    
    /// <summary>Returns a list of groups where the member is a part of.</summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="query">Pagination parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    [HttpGet("Member/{memberId:guid}", Name = "GetMemberGroups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<GroupSummary>>> GetMemberGroups(
        [FromRoute] Guid memberId, [FromQuery] GetMembersQuery query, CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching groups for members with MemberId={MemberId}", memberId);

        var groups = await groupMembershipService.GetGroupsAsync(memberId, cancellationToken);

        if (groups is null)
        {
            logger.LogWarning("Member not found with MemberId={MemberId}", memberId);
            return NotFound();
        }

        return Ok(groups);
    }
    
    /// <summary>Adds a member to a group, auto-enrolling them in all ancestor groups.</summary>
    /// <param name="request">The request body containing the member's ID and the group's ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    [HttpPost( Name = "AddMembership")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateGroupMembershipResponse>> AddMembership([FromBody] UpdateGroupMembershipRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Adding membership for MemberId={MemberId} to GroupId={GroupId}", request.MemberId, request.GroupId);

        var result = await groupMembershipService.AddMembershipAsync(request.GroupId, request.MemberId, cancellationToken);

        return result switch
        {
            UpdateGroupMembershipResult.Success => Ok(new UpdateGroupMembershipResponse { MemberId = request.MemberId, GroupId = request.GroupId }),
            UpdateGroupMembershipResult.GroupNotFound => NotFound("Group not found"),
            UpdateGroupMembershipResult.MemberNotFound => NotFound("Member not found"),
            UpdateGroupMembershipResult.AlreadyMember => Conflict("Member is already in this group"),
            _ => throw new Exception("Unexpected result from group service")
        };
    }
    
    /// <summary>Removes a group membership, auto-removing them from all child memberships.</summary>
    /// <param name="request">The request body containing the member's ID and the group's ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    [HttpDelete( Name = "RemoveMembership")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateGroupMembershipResponse>> RemoveMembership([FromBody] UpdateGroupMembershipRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Removing Membership for MemberId={MemberId} to GroupId={GroupId}", request.MemberId, request.GroupId);

        var result = await groupMembershipService.RemoveMembershipAsync(request.GroupId, request.MemberId, cancellationToken);

        return result switch
        {
            UpdateGroupMembershipResult.Success => Ok(new UpdateGroupMembershipResponse { MemberId = request.MemberId, GroupId = request.GroupId }),
            UpdateGroupMembershipResult.GroupNotFound => NotFound("Group not found"),
            UpdateGroupMembershipResult.MemberNotFound => NotFound("Member not found"),
            UpdateGroupMembershipResult.NotMember => Conflict("Member was not in this group"),
            _ => throw new Exception("Unexpected result from group service")
        };
    }
}