using VoluntroApi.Dtos.Members;
using VoluntroApi.Dtos.Shared;

namespace VoluntroApi.Services;

/// <summary>
/// Defines the contract for member management operations.
/// </summary>
public interface IMemberService
{
    /// <summary>
    /// Returns a paginated list of members ordered by last name then first name by default.
    /// </summary>
    /// <param name="query">Pagination and filter parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <param name="includeDeleted">When true, soft-deleted members are included.</param>
    /// <returns>A paged result of members.</returns>
    Task<PagedResult<MemberBriefDto>> GetAllAsync(GetMembersQuery query, CancellationToken cancellationToken, bool includeDeleted = false);

    /// <summary>
    /// Returns a single member by their unique identifier.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <param name="includeDeleted">When true, soft-deleted members are included.</param>
    /// <returns>The member, or null if not found.</returns>
    Task<MemberDto?> GetByIdAsync(Guid memberId, CancellationToken cancellationToken, bool includeDeleted = false);

    /// <summary>
    /// Creates a new member.
    /// </summary>
    /// <param name="request">The member details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created member.</returns>
    Task<MemberDto> CreateAsync(CreateMemberRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing member.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="request">The updated member details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated member, or null if not found.</returns>
    Task<MemberDto?> UpdateAsync(Guid memberId, UpdateMemberRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes a member by their unique identifier.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the member was deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid memberId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Permanently erases all personal data for a member (GDPR Article 17 right to erasure).
    /// The record is anonymized and soft-deleted; the ID is retained as a pseudonymous placeholder.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if erased, false if not found.</returns>
    Task<bool> GdprDeleteAsync(Guid memberId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Restores a soft-deleted member.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The restored member, or null if not found or not deleted.</returns>
    Task<MemberDto?> RestoreAsync(Guid memberId, CancellationToken cancellationToken);
}