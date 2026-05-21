using Microsoft.AspNetCore.Mvc;
using VoluntroApi.Dtos.Members;
using VoluntroApi.Dtos.Shared;
using VoluntroApi.Services;

namespace VoluntroApi.Controllers;

/// <summary>
/// Members are people connected to your church
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json", "application/xml")]
[Consumes("application/json")]
public class MembersController(
    IMemberService memberService,
    ILogger<MembersController> logger
    ) : ControllerBase
{

    /// <summary>
    /// Get all members
    /// </summary>
    /// <returns>List of Members</returns>
    [HttpGet(Name = "GetMembers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<MemberDto>>> GetMembers(
        [FromQuery] GetMembersQuery query, 
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching members on Page={Page} with PageSize={PageSize}", query.Page, query.PageSize);

        var members = await memberService.GetAllAsync(query, cancellationToken);

        logger.LogDebug("Returned {Count} members", members.TotalCount);

        return Ok(members);
    }

    /// <summary>
    /// Get member by memberId
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching member, or 404 if not found.</returns>
    [HttpGet("{memberId:guid}", Name = "GetMemberById")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDto>> GetMemberById(
        [FromRoute] Guid memberId,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching member with MemberId={MemberId}", memberId);

        var member = await memberService.GetByIdAsync(memberId, cancellationToken);

        if (member is null)
        {
            logger.LogWarning("Member not found MemberId={MemberId}", memberId);
            return NotFound();
        }

        return Ok(member);
    }

    /// <summary>
    /// Create a new member
    /// </summary>
    /// <param name="request">The member creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created member.</returns>
    [HttpPost(Name = "CreateMember")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MemberDto>> CreateMember(
        [FromBody]CreateMemberRequest request, 
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating member");

        var createdMember = await memberService.CreateAsync(request,  cancellationToken);

        logger.LogInformation("Member created MemberId={MemberId}", createdMember.Id);

        return CreatedAtAction(
            nameof(GetMemberById),
            new { memberId = createdMember.Id },
            createdMember
        );
    }

    /// <summary>
    /// Update member
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated member, or 404 if not found.</returns>
    [HttpPut("{memberId:guid}", Name = "UpdateMember")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDto>> UpdateMember(
        [FromRoute] Guid memberId,
        [FromBody] UpdateMemberRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Updating member MemberId={MemberId}", memberId);

        var updatedMember = await memberService.UpdateAsync(memberId, request,  cancellationToken);

        if (updatedMember is null)
        {
            logger.LogWarning("Member not found MemberId={MemberId}", memberId);
            return NotFound();
        }

        logger.LogInformation("Member updated MemberId={MemberId}", memberId);

        return Ok(updatedMember);
    }

    /// <summary>
    /// Deletes a member by its unique identifier.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content if deleted, 404 if not found.</returns>
    [HttpDelete("{memberId:guid}", Name = "DeleteMember")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteMember([FromRoute] Guid memberId, CancellationToken cancellationToken)
    {
        logger.LogDebug("Deleting member with MemberId={MemberId}", memberId);

        var deleted = await memberService.DeleteAsync(memberId, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning("Could not delete member. Member with MemberId={MemberId} not found.", memberId);
            return NotFound();
        }

        logger.LogInformation("Member with MemberId={MemberId} deleted.", memberId);

        return NoContent();
    }
}
