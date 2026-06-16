using Microsoft.EntityFrameworkCore;
using VoluntroApi.Data;
using VoluntroApi.Dtos.MemberTypes;
using VoluntroApi.Models;

namespace VoluntroApi.Services;

public class MemberTypeService(AppDbContext db, ILogger<MemberTypeService> logger): IMemberTypeService
{
    public async Task<List<MemberTypeDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting all member types");
        
        return await db.MemberTypes
            .OrderBy(mt => mt.Name)
            .Select(mt => new MemberTypeDto { Id = mt.Id, Name = mt.Name, IsDefault = mt.IsDefault })
            .ToListAsync(cancellationToken);
        
    }

    public async Task<MemberTypeDto> CreateAsync(CreateMemberTypeRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Creating member type with name {Name}", request.Name);

        var memberType = new MemberType
        {
            Name = request.Name.Trim(),
            IsDefault = false,
        };

        db.MemberTypes.Add(memberType);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Member type created MemberTypeId={MemberTypeId}", memberType.Id);

        return new MemberTypeDto { Id = memberType.Id, Name = memberType.Name, IsDefault = memberType.IsDefault };
    }

    public async Task<MemberTypeDto?> UpdateAsync(Guid id, UpdateMemberTypeRequest request, CancellationToken cancellationToken)
    {
        var memberType = await db.MemberTypes.FindAsync([id], cancellationToken);

        if (memberType is null)
        {
            logger.LogWarning("Update failed — member type not found MemberTypeId={MemberTypeId}", id);
            return null;
        }
        
        logger.LogInformation("MemberTypeId={MemberTypeId} updated name from {oldName} to {newName}", id, memberType.Name, request.Name);

        memberType.Name = request.Name.Trim();
        await db.SaveChangesAsync(cancellationToken);

        logger.LogDebug("MemberTypeId={MemberTypeId} updated", id);

        return new MemberTypeDto { Id = memberType.Id, Name = memberType.Name, IsDefault = memberType.IsDefault };
    }

    public async Task<DeleteMemberTypeResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var exists = await db.MemberTypes.AnyAsync(mt => mt.Id == id, cancellationToken);

        if (!exists)
        {
            logger.LogWarning("Delete failed — member type not found MemberTypeId={MemberTypeId}", id);
            return DeleteMemberTypeResult.NotFound;
        }

        var hasMembers = await db.Members.AnyAsync(m => m.MemberTypeId == id, cancellationToken);

        if (hasMembers)
        {
            logger.LogWarning("Delete failed — members still assigned MemberTypeId={MemberTypeId}", id);
            return DeleteMemberTypeResult.HasMembers;
        }

        var count = await db.MemberTypes.Where(mt => mt.Id == id).ExecuteDeleteAsync(cancellationToken);

        if (count <= 0) throw new Exception("Delete failed - unknown error");
       
        logger.LogInformation("Member type deleted MemberTypeId={MemberTypeId}", id); 
        return DeleteMemberTypeResult.Success;
    }

    public async Task<MemberTypeDto?> SetDefaultAsync(Guid id, CancellationToken cancellationToken)
    {
        var memberType = await db.MemberTypes.FindAsync([id], cancellationToken);

        if (memberType is null)
        {
            logger.LogWarning("SetDefault failed — member type not found MemberTypeId={MemberTypeId}", id);
            return null;
        }

        if (!memberType.IsDefault)
        {
            await db.MemberTypes
                .Where(mt => mt.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(mt => mt.IsDefault, false), cancellationToken);

            memberType.IsDefault = true;
            await db.SaveChangesAsync(cancellationToken);
            
        }
        
        logger.LogInformation("Default member type set MemberTypeId={MemberTypeId}", id);
        return new MemberTypeDto { Id = memberType.Id, Name = memberType.Name, IsDefault = memberType.IsDefault };
    }
}