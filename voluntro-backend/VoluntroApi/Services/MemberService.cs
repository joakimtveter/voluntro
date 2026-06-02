using Microsoft.EntityFrameworkCore;
using VoluntroApi.Data;
using VoluntroApi.Dtos.Members;
using VoluntroApi.Dtos.Shared;
using VoluntroApi.Models;

namespace VoluntroApi.Services;

/// <summary>
/// EF Core-backed implementation of <see cref="IMemberService"/>.
/// </summary>
public class MemberService(AppDbContext db, ILogger<MemberService> logger) : IMemberService
{
    /// <inheritdoc/>
    public async Task<PagedResult<MemberBriefDto>> GetAllAsync(GetMembersQuery query,
        CancellationToken cancellationToken, bool includeDeleted = false)
    {
        logger.LogDebug("Querying members Page={Page} PageSize={PageSize} IncludeDeleted={IncludeDeleted}", query.Page, query.PageSize, includeDeleted);

        return await db.Members
            .AsNoTracking()
            .Where(m => includeDeleted || !m.IsDeleted)
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToPagedResultAsync(m => new MemberBriefDto
            {
                Id = m.Id,
                FirstName = m.FirstName,
                MiddleNames = m.MiddleNames,
                LastName = m.LastName,
                DateOfBirth = m.DateOfBirth,
                LegalGender = m.LegalGender,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
                IsDeleted = m.IsDeleted,
            }, query.Page, query.PageSize, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<MemberDto?> GetByIdAsync(Guid memberId, CancellationToken cancellationToken, bool includeDeleted = false)
    {
        logger.LogDebug("Querying member MemberId={MemberId} IncludeDeleted={IncludeDeleted}", memberId, includeDeleted);

        var member = await db.Members
            .AsNoTracking()
            .Where(m => m.Id == memberId && (includeDeleted || !m.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        return member is null ? null : ToDto(member);
    }
    
    /// <inheritdoc/>
    public async Task<MemberDto> CreateAsync(CreateMemberRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating member");

        var now = DateTimeOffset.UtcNow;
        var member = new Member
        {
            FirstName = request.FirstName.Trim(),
            MiddleNames = request.MiddleNames?.Trim(),
            LastName = request.LastName.Trim(),
            DateOfBirth = request.DateOfBirth,
            LegalGender = request.LegalGender,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Members.Add(member);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(member);
    }

    /// <inheritdoc/>
    public async Task<MemberDto?> UpdateAsync(Guid memberId, UpdateMemberRequest request, CancellationToken cancellationToken)
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
        member.DateOfBirth = request.DateOfBirth;
        member.LegalGender = request.LegalGender;
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
        member.DateOfBirth = null;
        member.LegalGender = default;
        member.IsDeleted = true;
        member.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("GDPR erasure completed MemberId={MemberId}", memberId);
        return true;
    }
    
    /// <inheritdoc/>
    public async Task<MemberDto?> RestoreAsync(Guid memberId, CancellationToken cancellationToken)
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


    private static MemberDto ToDto(Member m) => new()
    {
        Id = m.Id,
        FirstName = m.FirstName,
        MiddleNames = m.MiddleNames,
        LastName = m.LastName,
        DateOfBirth = m.DateOfBirth,
        LegalGender = m.LegalGender,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt,
        IsDeleted = m.IsDeleted,
    };
}
