using Microsoft.EntityFrameworkCore;
using VoluntroApi.Data;
using VoluntroApi.Dtos.Members;
using VoluntroApi.Dtos.Shared;
using VoluntroApi.Dtos.Tags;
using VoluntroApi.Models;

namespace VoluntroApi.Services;

/// <summary>
/// EF Core-backed implementation of <see cref="IMemberService"/>.
/// </summary>
public class MemberService(AppDbContext db, ILogger<MemberService> logger) : IMemberService
{
    /// <inheritdoc/>
    public async Task<PagedResult<MemberSummary>> GetAllAsync(GetMembersQuery query,
        CancellationToken cancellationToken, bool includeDeleted = false)
    {
        logger.LogDebug("Querying members Page={Page} PageSize={PageSize} IncludeDeleted={IncludeDeleted} TagIds={TagIds}", query.Page, query.PageSize, includeDeleted, query.TagIds);

        var hasTagFilter = query.TagIds is { Count: > 0 };
        var matchAll = query.TagFilterMode == TagFilterMode.All;

        var baseQuery = db.Members
            .AsNoTracking()
            .Where(m => includeDeleted || !m.IsDeleted)
            .Where(m => !hasTagFilter
                        || (matchAll
                            ? query.TagIds!.All(tagId => m.MemberTags.Any(mt => mt.TagId == tagId))
                            : m.MemberTags.Any(mt => query.TagIds!.Contains(mt.TagId))))
            .Where(m => query.LegalGender == null || m.LegalGender == query.LegalGender);

        const string nb = "Norwegian_100_CI_AS";

        var orderedQuery = (query.SortBy, query.SortOrder) switch
        {
            (MemberSortBy.FirstName, SortDirection.Asc) => baseQuery.OrderBy(m => EF.Functions.Collate(m.FirstName, nb)).ThenBy(m => EF.Functions.Collate(m.LastName, nb)),
            (MemberSortBy.FirstName, SortDirection.Desc) => baseQuery.OrderByDescending(m => EF.Functions.Collate(m.FirstName, nb)).ThenByDescending(m => EF.Functions.Collate(m.LastName, nb)),
            (MemberSortBy.DateOfBirth, SortDirection.Asc) => baseQuery.OrderBy(m => m.DateOfBirth),
            (MemberSortBy.DateOfBirth, SortDirection.Desc) => baseQuery.OrderByDescending(m => m.DateOfBirth),
            (MemberSortBy.LastName, SortDirection.Desc) => baseQuery.OrderByDescending(m => EF.Functions.Collate(m.LastName, nb)).ThenByDescending(m => EF.Functions.Collate(m.FirstName, nb)),
            _ => baseQuery.OrderBy(m => EF.Functions.Collate(m.LastName, nb)).ThenBy(m => EF.Functions.Collate(m.FirstName, nb)),
        };

        return await orderedQuery
            .ToPagedResultAsync(m => new MemberSummary
            {
                Id = m.Id,
                FirstName = m.FirstName,
                MiddleNames = m.MiddleNames,
                LastName = m.LastName,
                Email = m.Email,
                DateOfBirth = m.DateOfBirth,
                Tags = m.MemberTags.Select(mt => new TagDto { Id = mt.Tag.Id, Name = mt.Tag.Name, Color = mt.Tag.Color }).ToList(),
                LegalGender = m.LegalGender,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
                IsDeleted = m.IsDeleted,
                MemberTypeId = m.MemberTypeId,
            }, query.Page, query.PageSize, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<MemberDetails?> GetByIdAsync(Guid memberId, CancellationToken cancellationToken, bool includeDeleted = false)
    {
        logger.LogDebug("Querying member MemberId={MemberId} IncludeDeleted={IncludeDeleted}", memberId, includeDeleted);

        var member = await db.Members
            .AsNoTracking()
            .Include(m => m.MemberTags)
            .ThenInclude(mt => mt.Tag)
            .Where(m => m.Id == memberId && (includeDeleted || !m.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        return member is null ? null : ToDto(member);
    }
    
    /// <inheritdoc/>
    public async Task<MemberDetails> CreateAsync(CreateMemberRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating member");

        var memberTypeId = request.MemberTypeId
            ?? await db.MemberTypes
                .Where(mt => mt.IsDefault)
                .Select(mt => mt.Id)
                .FirstOrDefaultAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var member = new Member
        {
            FirstName = request.FirstName.Trim(),
            MiddleNames = request.MiddleNames?.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email?.Trim(),
            DateOfBirth = request.DateOfBirth,
            LegalGender = request.LegalGender,
            MemberTypeId = memberTypeId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Members.Add(member);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(member);
    }

    /// <inheritdoc/>
    public async Task<MemberDetails?> UpdateAsync(Guid memberId, UpdateMemberRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Updating member MemberId={MemberId}", memberId);

        var member = await db.Members.FindAsync([memberId], cancellationToken);

        if (member is null)
        {
            logger.LogWarning("Update failed — member not found MemberId={MemberId}", memberId);
            return null;
        }

        member.FirstName = request.FirstName.Trim();
        member.MiddleNames = request.MiddleNames?.Trim();
        member.LastName = request.LastName.Trim();
        member.Email = request.Email?.Trim();
        member.DateOfBirth = request.DateOfBirth;
        member.LegalGender = request.LegalGender;
        if (request.MemberTypeId.HasValue)
            member.MemberTypeId = request.MemberTypeId.Value;
        member.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return ToDto(member);
    }
    
    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var member = await db.Members.FindAsync([memberId], cancellationToken);

        if (member is null || member.IsDeleted)
        {
            logger.LogWarning("Delete failed — member not found MemberId={MemberId}", memberId);
            return false;
        }

        member.IsDeleted = true;
        member.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> GdprDeleteAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var member = await db.Members.FindAsync([memberId], cancellationToken);

        if (member is null)
        {
            logger.LogWarning("GDPR delete failed — member not found MemberId={MemberId}", memberId);
            return false;
        }

        member.FirstName = "[deleted]";
        member.MiddleNames = null;
        member.LastName = "[deleted]";
        member.Email = $"deleted+{member.Id:N}@voluntro.local";
        member.DateOfBirth = null;
        member.LegalGender = default;
        member.IsDeleted = true;
        member.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("GDPR erasure completed MemberId={MemberId}", memberId);
        return true;
    }
    
    /// <inheritdoc/>
    public async Task<MemberDetails?> RestoreAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var member = await db.Members.FindAsync([memberId], cancellationToken);

        if (member is null || !member.IsDeleted)
        {
            logger.LogWarning("Restore failed — member not found or not deleted MemberId={MemberId}", memberId);
            return null;
        }

        member.IsDeleted = false;
        member.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Member restored MemberId={MemberId}", memberId);
        return ToDto(member);
    }

    /// <inheritdoc/>
    public async Task<AddTagResult> AddTagAsync(Guid memberId, Guid tagId, CancellationToken cancellationToken)
    {
        var memberExists = await db.Members.AnyAsync(m => m.Id == memberId, cancellationToken);
        
        if (!memberExists)
        {
            logger.LogWarning("AddTag failed — member not found MemberId={MemberId}", memberId);
            return AddTagResult.MemberNotFound;
        }
        
        var tagExists = await db.Tags.AnyAsync(t => t.Id == tagId, cancellationToken);
        
        if (!tagExists)
        {
            logger.LogWarning("AddTag failed — tag not found TagId={TagId}", tagId);
            return AddTagResult.TagNotFound;
        }
        
        var alreadyTagged = await db.MemberTags.AnyAsync(mt => mt.MemberId == memberId && mt.TagId == tagId, cancellationToken);

        if (alreadyTagged)
        {
            logger.LogWarning("AddTag failed — tag already assigned MemberId={MemberId} TagId={TagId}", memberId, tagId);
            return AddTagResult.AlreadyTagged;
        }

        db.MemberTags.Add(new MemberTag { MemberId = memberId, TagId = tagId });
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Tag added MemberId={MemberId} TagId={TagId}", memberId, tagId);
        return AddTagResult.Success;
    }
    
    /// <inheritdoc/>
    public async Task<RemoveTagResult> RemoveTagAsync(Guid memberId, Guid tagId, CancellationToken cancellationToken)
    {
        var memberExists = await db.Members.AnyAsync(m => m.Id == memberId, cancellationToken);
        
        if (!memberExists)
        {
            logger.LogWarning("AddTag failed — member not found MemberId={MemberId}", memberId);
            return RemoveTagResult.MemberNotFound;
        }
        
        var tagExists = await db.Tags.AnyAsync(t => t.Id == tagId, cancellationToken);
        
        if (!tagExists)
        {
            logger.LogWarning("AddTag failed — tag not found TagId={TagId}", tagId);
            return RemoveTagResult.TagNotFound;
        }

        var count = await db.MemberTags.Where(mt => mt.MemberId == memberId && mt.TagId == tagId).ExecuteDeleteAsync(cancellationToken);
        return count > 0 ? RemoveTagResult.Success : RemoveTagResult.TagNotFound;
    }


    private static MemberDetails ToDto(Member m) => new()
    {
        Id = m.Id,
        FirstName = m.FirstName,
        MiddleNames = m.MiddleNames,
        LastName = m.LastName,
        Email = m.Email,
        DateOfBirth = m.DateOfBirth,
        Tags = m.MemberTags.Select(mt => new TagDto { Id = mt.Tag.Id, Name = mt.Tag.Name, Color = mt.Tag.Color }).ToList(),
        LegalGender = m.LegalGender,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt,
        IsDeleted = m.IsDeleted,
        MemberTypeId = m.MemberTypeId,
    };
}
