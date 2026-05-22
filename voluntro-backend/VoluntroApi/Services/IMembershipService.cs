using VoluntroApi.Dtos.Groups;
using VoluntroApi.Dtos.Members;
using VoluntroApi.Dtos.Memberships;
using VoluntroApi.Dtos.Shared;

namespace VoluntroApi.Services;

/// <summary>
/// Defines the contract for membership management operations.
/// </summary>
public interface IMembershipService
{
    /// <summary>
    /// Returns a paginated list of active members belonging to a group, or <c>null</c> if the group is not found.
    /// </summary>
    /// <param name="groupId">The member's unique identifier.</param>
    /// <param name="query">The member's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task<PagedResult<MemberBriefDto>?> GetMembersAsync(Guid groupId, GetMembersQuery query, CancellationToken cancellationToken);
    
    /// <summary>
    /// Returns a list of groups the member is a part of.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>List of groups the member is a part of, or null if not found.</returns>
    Task<List<GroupBriefDto>?> GetGroupsAsync(Guid memberId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a member to a group and auto-enrolls them in all ancestor groups.
    /// </summary>
    Task<UpdateMembershipResult> AddMembershipAsync(Guid groupId, Guid memberId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Adds a member to a group and auto-enrolls them in all ancestor groups.
    /// </summary>
    Task<UpdateMembershipResult> RemoveMembershipAsync(Guid groupId, Guid memberId, CancellationToken cancellationToken);
}
