using Microsoft.EntityFrameworkCore;
using VoluntroApi.Data;
using VoluntroApi.Dtos.Groups;
using VoluntroApi.Dtos.Members;
using VoluntroApi.Dtos.Memberships;
using VoluntroApi.Dtos.Shared;
using VoluntroApi.Models;

namespace VoluntroApi.Services;

/// <summary>
/// EF Core-backed implementation of <see cref="IMembershipService"/>.
/// </summary>
public class MembershipService(AppDbContext db, ILogger<GroupService> logger) : IMembershipService
{
    /// <inheritdoc/>
    public async Task<PagedResult<MemberBriefDto>?> GetMembersAsync(Guid groupId, GetMembersQuery query, CancellationToken cancellationToken)
    {
        var groupExists = await db.Groups.AnyAsync(g => g.Id == groupId && !g.IsDeleted, cancellationToken);
        if (!groupExists)
        {
            logger.LogWarning("GetMembers failed — group not found GroupId={GroupId}", groupId);
            return null;
        }
        
        return await db.MemberGroups
            .AsNoTracking()
            .Where(mg => mg.GroupId == groupId && !mg.Member.IsDeleted)
            .OrderBy(mg => mg.Member.LastName)
            .ThenBy(mg => mg.Member.FirstName)
            .ToPagedResultAsync(mg => new MemberBriefDto
            {
                Id = mg.Member.Id,
                FirstName = mg.Member.FirstName,
                MiddleNames = mg.Member.MiddleNames,
                LastName = mg.Member.LastName,
                DateOfBirth = mg.Member.DateOfBirth,
                LegalGender = mg.Member.LegalGender,
                CreatedAt = mg.Member.CreatedAt,
                UpdatedAt = mg.Member.UpdatedAt,
                IsDeleted = mg.Member.IsDeleted,
            }, query.Page, query.PageSize, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<GroupBriefDto>?> GetGroupsAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var memberExists = await db.Members.AnyAsync(m => m.Id == memberId && !m.IsDeleted, cancellationToken);
        if (!memberExists) return null;
        
        return await db.MemberGroups
            .AsNoTracking()
            .Where(mg => mg.MemberId == memberId)
            .Select(mg => new GroupBriefDto
            {
                Id = mg.Group.Id,
                Name = mg.Group.Name,
                ParentGroupId = mg.Group.ParentGroupId,
                CreatedAt = mg.Group.CreatedAt,
                UpdatedAt = mg.Group.UpdatedAt,
                IsDeleted = mg.Group.IsDeleted,
            })
            .ToListAsync(cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<UpdateMembershipResult> AddMembershipAsync(Guid groupId, Guid memberId, CancellationToken cancellationToken)
    {
        logger.LogDebug("Adding member MemberId={MemberId} to group GroupId={GroupId}", memberId, groupId);

        var groupExists = await db.Groups.AnyAsync(g => g.Id == groupId && !g.IsDeleted, cancellationToken);
        if (!groupExists)
        {
            logger.LogWarning("AddMember failed — group not found GroupId={GroupId}", groupId);
            return UpdateMembershipResult.GroupNotFound;
        }

        var memberExists = await db.Members.AnyAsync(m => m.Id == memberId && !m.IsDeleted, cancellationToken);
        if (!memberExists)
        {
            logger.LogWarning("AddMember failed — member not found MemberId={MemberId}", memberId);
            return UpdateMembershipResult.MemberNotFound;
        }

        var alreadyMember = await db.MemberGroups.AnyAsync(mg => mg.GroupId == groupId && mg.MemberId == memberId, cancellationToken);
        if (alreadyMember)
        {
            logger.LogWarning("AddMember failed — member MemberId={MemberId} is already in GroupId={GroupId}", memberId, groupId);
            return UpdateMembershipResult.AlreadyMember;
        }

        // Collect the target group and all its ancestors.
        var groupsToEnroll = new List<Guid>();
        var current = (Guid?)groupId;
        while (current.HasValue)
        {
            var currentGroupId = current.Value;
            
            groupsToEnroll.Add(currentGroupId);
            
            current = await db.Groups
                .AsNoTracking()
                .Where(g => g.Id == currentGroupId)
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
            .Select(gid => new MemberGroup
            {
                MemberId = memberId,
                GroupId = gid,
                JoinedAt = now
            })
            .ToList();

        db.MemberGroups.AddRange(newRows);
        
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Member MemberId={MemberId} added to GroupId={GroupId} and {AncestorCount} ancestor group(s)",
            memberId, groupId, groupsToEnroll.Count - 1);

        return UpdateMembershipResult.Success;
    }

    /// <inheritdoc/>
    public async Task<UpdateMembershipResult> RemoveMembershipAsync(Guid groupId, Guid memberId, CancellationToken cancellationToken)
    {
        var groupExists = await db.Groups.AnyAsync(g => g.Id == groupId && !g.IsDeleted, cancellationToken);
        if (!groupExists)
        {
            logger.LogWarning("Delete membership failed — group not found: GroupId={GroupId}", groupId);
            return UpdateMembershipResult.GroupNotFound;
        }
        
        var memberExists = await db.Members.AnyAsync(m => m.Id == memberId && !m.IsDeleted, cancellationToken);
        if (!memberExists)
        {
            logger.LogWarning("Delete membership failed — member not found: MemberId={MemberId}", memberId);
            return UpdateMembershipResult.MemberNotFound;
        }
        
        var groupsToRemove = new List<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(groupId);
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            groupsToRemove.Add(current);

            var childIds = await db.Groups
                .Where(g => g.ParentGroupId == current && !g.IsDeleted)
                .Select(g => g.Id)
                .ToListAsync(cancellationToken);

            foreach (var childId in childIds)
                queue.Enqueue(childId);
        }

        var deleted = await db.MemberGroups
            .Where(mg => groupsToRemove.Contains(mg.GroupId) && mg.MemberId == memberId)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted == 0)
        {
            logger.LogWarning("Delete membership failed — membership not found: MemberId={MemberId} GroupId={GroupId}", memberId, groupId);
            return UpdateMembershipResult.NotMember;
        }

        logger.LogInformation("Removed MemberId={MemberId} from GroupId={GroupId}", memberId, groupId);
        return UpdateMembershipResult.Success;
    }
}