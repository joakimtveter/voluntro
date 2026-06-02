using VoluntroApi.Dtos.Groups;
using VoluntroApi.Dtos.Shared;

namespace VoluntroApi.Services;

/// <summary>
/// Defines the contract for group management operations.
/// </summary>
public interface IGroupService
{
    /// <summary>
    /// Returns a paginated list of groups ordered by name.
    /// When no <see cref="GetGroupsQuery.ParentGroupId"/> is set, only root groups are returned.
    /// </summary>
    Task<PagedResult<GroupBriefDto>> GetAllAsync(GetGroupsQuery query, CancellationToken cancellationToken, bool includeDeleted);

    /// <summary>
    /// Returns a single group with its direct children and active member count, or <c>null</c> if not found.
    /// </summary>
    Task<GroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken, bool includeDeleted);

    /// <summary>
    /// Creates a new group. Returns <c>null</c> if the specified parent group does not exist or is deleted.
    /// </summary>
    Task<GroupDto?> CreateAsync(CreateGroupRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing group. Returns <c>null</c> if not found, parent not found, or if the new parent would create a cycle.
    /// </summary>
    Task<(GroupDto?, UpdateGroupResult)> UpdateAsync(Guid id, UpdateGroupRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes a group. Returns <see cref="DeleteGroupResult.HasChildren"/> if active child groups exist.
    /// </summary>
    Task<DeleteGroupResult> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Restores a soft-deleted group. Returns <c>null</c> if not found or not currently deleted.
    /// </summary>
    Task<(GroupDto?, RestoreGroupResult)> RestoreAsync(Guid id, CancellationToken cancellationToken);
}