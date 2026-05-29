using Microsoft.AspNetCore.Mvc;
using VoluntroApi.Dtos.Groups;
using VoluntroApi.Dtos.Members;
using VoluntroApi.Dtos.Memberships;
using VoluntroApi.Services;

namespace VoluntroApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Consumes("application/json")]
[Produces("application/json", "application/xml")]
public class MembershipsController(IMembershipService membershipService, ILogger<GroupsController> logger) : ControllerBase
{
    /// <summary>Returns a paginated list of members belonging to a group.</summary>
    /// <param name="groupId">The group's unique identifier.</param>
    /// <param name="query">Pagination parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    [HttpGet("Groups/{groupId:guid}", Name = "GetGroupMembers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberBriefDto>> GetGroupMembers(
        [FromRoute] Guid groupId, [FromQuery] GetMembersQuery query, CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching members for GroupId={GroupId}", groupId);

        var members = await membershipService.GetMembersAsync(groupId, query, cancellationToken);

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
    public async Task<ActionResult<List<GroupBriefDto>>> GetMemberGroups(
        [FromRoute] Guid memberId, [FromQuery] GetMembersQuery query, CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching groups for members with MemberId={MemberId}", memberId);

        var groups = await membershipService.GetGroupsAsync(memberId, cancellationToken);

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
    public async Task<ActionResult<UpdateMembershipResponseDto>> AddMembership([FromBody] UpdateMembershipRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Adding membership for MemberId={MemberId} to GroupId={GroupId}", request.MemberId, request.GroupId);

        var result = await membershipService.AddMembershipAsync(request.GroupId, request.MemberId, cancellationToken);

        return result switch
        {
            UpdateMembershipResult.Success => Ok(new UpdateMembershipResponseDto { MemberId = request.MemberId, GroupId = request.GroupId }),
            UpdateMembershipResult.GroupNotFound => NotFound("Group not found"),
            UpdateMembershipResult.MemberNotFound => NotFound("Member not found"),
            UpdateMembershipResult.AlreadyMember => Conflict("Member is already in this group"),
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
    public async Task<ActionResult<UpdateMembershipResponseDto>> RemoveMembership([FromBody] UpdateMembershipRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Removing Membership for MemberId={MemberId} to GroupId={GroupId}", request.MemberId, request.GroupId);

        var result = await membershipService.RemoveMembershipAsync(request.GroupId, request.MemberId, cancellationToken);

        return result switch
        {
            UpdateMembershipResult.Success => Ok(new UpdateMembershipResponseDto { MemberId = request.MemberId, GroupId = request.GroupId }),
            UpdateMembershipResult.GroupNotFound => NotFound("Group not found"),
            UpdateMembershipResult.MemberNotFound => NotFound("Member not found"),
            UpdateMembershipResult.NotMember => Conflict("Member was not in this group"),
            _ => throw new Exception("Unexpected result from group service")
        };
    }
}