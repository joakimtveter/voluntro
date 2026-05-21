using Microsoft.AspNetCore.Mvc;
using VoluntroApi.Dtos.Members;
using VoluntroApi.Dtos.Shared;
using VoluntroApi.Services;

namespace VoluntroApi.Controllers;

/// <summary>
/// Admin operations for member management, including soft-deleted members.
/// </summary>
[ApiController]
[Route("api/admin/members")]
[Produces("application/json", "application/xml")]
[Consumes("application/json")]
public class AdminMembersController(
    IMemberService memberService,
    ILogger<AdminMembersController> logger
) : ControllerBase
{
    /// <summary>
    /// Returns a paginated list of all members, including soft-deleted ones.
    /// </summary>
    /// <param name="query">Pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result of all members.</returns>
    [HttpGet(Name = "AdminGetMembers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<MemberBriefDto>>> GetMembers(
        [FromQuery] AdminGetMembersQuery query,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Admin: fetching all members Page={Page} PageSize={PageSize} IncludeDeleted={IncludeDeleted}", query.Page, query.PageSize, query.IncludeDeleted);

        var members = await memberService.GetAllAsync(query, cancellationToken, query.IncludeDeleted);

        logger.LogDebug("Admin: returned {TotalCount} members across {TotalPages} pages", members.TotalCount, members.TotalPages);

        return Ok(members);
    }

    /// <summary>
    /// Returns a single member by their unique identifier, including soft-deleted members.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The member, or 404 if not found.</returns>
    [HttpGet("{memberId:guid}", Name = "AdminGetMemberById")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDto>> GetMemberById(
        [FromRoute] Guid memberId,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Admin: fetching member MemberId={MemberId}", memberId);

        var member = await memberService.GetByIdAsync(memberId, cancellationToken, includeDeleted: true);

        if (member is null)
        {
            logger.LogWarning("Admin: member not found MemberId={MemberId}", memberId);
            return NotFound();
        }

        return Ok(member);
    }

    /// <summary>
    /// Restores a soft-deleted member.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The restored member, or 404 if not found or not deleted.</returns>
    [HttpPost("{memberId:guid}/restore", Name = "AdminRestoreMember")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDto>> RestoreMember(
        [FromRoute] Guid memberId,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Admin: restoring member MemberId={MemberId}", memberId);

        var restored = await memberService.RestoreAsync(memberId, cancellationToken);

        if (restored is null)
        {
            logger.LogWarning("Admin: restore failed — member not found or not deleted MemberId={MemberId}", memberId);
            return NotFound();
        }

        logger.LogInformation("Admin: member restored MemberId={MemberId}", memberId);
        return Ok(restored);
    }

    /// <summary>
    /// Permanently erases all personal data for a member (GDPR Article 17 right to erasure).
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content if erased, 404 if not found.</returns>
    [HttpDelete("{memberId:guid}/gdpr", Name = "AdminGdprDeleteMember")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GdprDeleteMember(
        [FromRoute] Guid memberId,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Admin: GDPR erasure request for MemberId={MemberId}", memberId);

        var erased = await memberService.GdprDeleteAsync(memberId, cancellationToken);

        if (!erased)
        {
            logger.LogWarning("Admin: GDPR delete failed — member not found MemberId={MemberId}", memberId);
            return NotFound();
        }

        logger.LogInformation("Admin: GDPR erasure completed MemberId={MemberId}", memberId);
        return NoContent();
    }
}
