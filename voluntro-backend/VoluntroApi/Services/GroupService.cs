using Microsoft.EntityFrameworkCore;
using VoluntroApi.Data;
using VoluntroApi.Dtos.Groups;
using VoluntroApi.Dtos.Shared;
using VoluntroApi.Models;

namespace VoluntroApi.Services;

/// <summary>
/// EF Core-backed implementation of <see cref="IGroupService"/>.
/// </summary>
public class GroupService(AppDbContext db, ILogger<GroupService> logger) : IGroupService
{
    /// <inheritdoc/>
    public async Task<PagedResult<GroupBriefDto>> GetAllAsync(GetGroupsQuery query, 
        CancellationToken cancellationToken, bool includeDeleted = false)
    {
        logger.LogDebug("Querying groups Page={Page} PageSize={PageSize} IncludeDeleted={IncludeDeleted}", query.Page, query.PageSize, includeDeleted);
        
        return await db.Groups
            .AsNoTracking()
            .Where(g => !query.ParentGroupId.HasValue || g.ParentGroupId == query.ParentGroupId)
            .Where(g => includeDeleted || !g.IsDeleted )
            .OrderBy(g => g.ParentGroupId)
            .ThenBy(g => g.Name)
            .ToPagedResultAsync(g => new GroupBriefDto
            {
                Id = g.Id,
                Name = g.Name,
                ParentGroupId = g.ParentGroupId,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt,
                IsDeleted = g.IsDeleted
            }, query.Page, query.PageSize, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken, bool includeDeleted)
    {
        logger.LogDebug("Querying group GroupId={GroupId} IncludeDeleted={IncludeDeleted}", id, includeDeleted);

        return await db.Groups
            .AsNoTracking()
            .Where(g => g.Id == id && (includeDeleted || !g.IsDeleted))
            .Select(g => new GroupDto
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                ParentGroupId = g.ParentGroupId,
                ParentGroupName = g.ParentGroup != null ? g.ParentGroup.Name : null,
                ChildGroups = g.ChildGroups
                    .Where(c => !c.IsDeleted)
                    .Select(c => new GroupBriefDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        ParentGroupId = c.ParentGroupId,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        IsDeleted = c.IsDeleted,
                    })
                    .ToList(),
                MemberCount = g.MemberGroups.Count(mg => !mg.Member.IsDeleted),
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt,
                IsDeleted = g.IsDeleted,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GroupDto?> CreateAsync(CreateGroupRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating group Name={Name}", request.Name);

        if (request.ParentGroupId.HasValue)
        {
            var parentExists = await db.Groups
                .AnyAsync(g => g.Id == request.ParentGroupId && !g.IsDeleted, cancellationToken);

            if (!parentExists)
            {
                logger.LogWarning("Create failed — parent group not found ParentGroupId={ParentGroupId}", request.ParentGroupId);
                return null;
            }
        }

        var now = DateTimeOffset.UtcNow;
        var group = new Group
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ParentGroupId = request.ParentGroupId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Groups.Add(group);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Group created GroupId={GroupId}", group.Id);
        return await GetByIdAsync(group.Id, cancellationToken, includeDeleted: false);
    }

    /// <inheritdoc/>
    public async Task<(GroupDto?, UpdateGroupResult)> UpdateAsync(Guid id, UpdateGroupRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Updating group GroupId={GroupId}", id);

        var group = await db.Groups.FindAsync([id], cancellationToken);

        if (group is null || group.IsDeleted)
        {
            logger.LogWarning("Update failed — group not found GroupId={GroupId}", id);
            return (null, UpdateGroupResult.NotFound);
        }

        if (request.ParentGroupId.HasValue)
        {
            if (request.ParentGroupId == id || await WouldCreateCycleAsync(id, request.ParentGroupId.Value, cancellationToken))
            {
                logger.LogWarning("Update failed — re-parenting would create a cycle GroupId={GroupId} ProposedParentId={ProposedParentId}", id, request.ParentGroupId);
                return (null, UpdateGroupResult.CycleDetected);
            }

            var parentExists = await db.Groups
                .AnyAsync(g => g.Id == request.ParentGroupId && !g.IsDeleted, cancellationToken);

            if (!parentExists)
            {
                logger.LogWarning("Update failed — parent group not found ParentGroupId={ParentGroupId}", request.ParentGroupId);
                return (null, UpdateGroupResult.ParentNotFound);
            }
        }

        group.Name = request.Name.Trim();
        group.Description = request.Description?.Trim();
        group.ParentGroupId = request.ParentGroupId;
        group.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Group updated GroupId={GroupId}", id);
        return (await GetByIdAsync(id, cancellationToken, includeDeleted: false), UpdateGroupResult.Success);
    }

    private async Task<bool> WouldCreateCycleAsync(Guid groupId, Guid proposedParentId, CancellationToken cancellationToken)
    {
        var current = (Guid?)proposedParentId;
        while (current.HasValue)
        {
            if (current == groupId) return true;
            current = await db.Groups
                .AsNoTracking()
                .Where(g => g.Id == current.Value)
                .Select(g => g.ParentGroupId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        return false;
    }
    
    /// <inheritdoc/>
    public async Task<DeleteGroupResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var groupToDelete = await db.Groups.FindAsync([id], cancellationToken);
        if (groupToDelete is null) return DeleteGroupResult.NotFound;
        
        var hasChildren = await db.Groups
            .AnyAsync(g => g.ParentGroupId == id && !g.IsDeleted, cancellationToken);

        if (hasChildren) return DeleteGroupResult.HasChildren;
        
        groupToDelete.IsDeleted = true;
        groupToDelete.UpdatedAt = DateTimeOffset.UtcNow;
        
        await db.SaveChangesAsync(cancellationToken);
        
        return DeleteGroupResult.Success;
    }

    /// <inheritdoc/>
    public async Task<AddMemberToGroupResult> AddMemberAsync(Guid groupId, Guid memberId, CancellationToken cancellationToken)
    {
        logger.LogDebug("Adding member MemberId={MemberId} to group GroupId={GroupId}", memberId, groupId);

        var groupExists = await db.Groups.AnyAsync(g => g.Id == groupId && !g.IsDeleted, cancellationToken);
        if (!groupExists)
        {
            logger.LogWarning("AddMember failed — group not found GroupId={GroupId}", groupId);
            return AddMemberToGroupResult.GroupNotFound;
        }

        var memberExists = await db.Members.AnyAsync(m => m.Id == memberId && !m.IsDeleted, cancellationToken);
        if (!memberExists)
        {
            logger.LogWarning("AddMember failed — member not found MemberId={MemberId}", memberId);
            return AddMemberToGroupResult.MemberNotFound;
        }

        var alreadyMember = await db.MemberGroups.AnyAsync(mg => mg.GroupId == groupId && mg.MemberId == memberId, cancellationToken);
        if (alreadyMember)
        {
            logger.LogWarning("AddMember failed — member MemberId={MemberId} is already in GroupId={GroupId}", memberId, groupId);
            return AddMemberToGroupResult.AlreadyMember;
        }

        // Collect the target group and all its ancestors.
        var groupsToEnroll = new List<Guid>();
        var current = (Guid?)groupId;
        while (current.HasValue)
        {
            groupsToEnroll.Add(current.Value);
            current = await db.Groups
                .AsNoTracking()
                .Where(g => g.Id == current.Value)
                .Select(g => g.ParentGroupId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Only insert rows that don't already exist (member may already be in ancestor groups).
        var existingGroupIds = await db.MemberGroups
            .Where(mg => mg.MemberId == memberId && groupsToEnroll.Contains(mg.GroupId))
            .Select(mg => mg.GroupId)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var newRows = groupsToEnroll
            .Except(existingGroupIds)
            .Select(gid => new MemberGroup { MemberId = memberId, GroupId = gid, JoinedAt = now });

        db.MemberGroups.AddRange(newRows);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Member MemberId={MemberId} added to GroupId={GroupId} and {AncestorCount} ancestor group(s)",
            memberId, groupId, groupsToEnroll.Count - 1);

        return AddMemberToGroupResult.Success;
    }

    /// <inheritdoc/>
    public async Task<(GroupDto?, RestoreGroupResult)> RestoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var groupToRestore = await db.Groups.FindAsync([id], cancellationToken);
        if (groupToRestore is null) return (null, RestoreGroupResult.NotFound);
        if (!groupToRestore.IsDeleted) return (null, RestoreGroupResult.AlreadyRestored);

        if (groupToRestore.ParentGroupId.HasValue)
        {
            var parentExists = await db.Groups
                .AnyAsync(g => g.Id == groupToRestore.ParentGroupId && !g.IsDeleted, cancellationToken);

            if (!parentExists) return (null, RestoreGroupResult.ParentDoesNotExist);
        }
        
        groupToRestore.IsDeleted = false;
        groupToRestore.UpdatedAt = DateTimeOffset.UtcNow;
        
        await db.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(id, cancellationToken, false), RestoreGroupResult.Success);
    }
}