using VoluntroApi.Dtos.MemberTypes;

namespace VoluntroApi.Services;

public interface IMemberTypeService
{
    Task<List<MemberTypeDto>> GetAllAsync(CancellationToken cancellationToken);
    
    Task<MemberTypeDto> CreateAsync(CreateMemberTypeRequest request, CancellationToken cancellationToken);
    
    Task<MemberTypeDto?> UpdateAsync(Guid id, UpdateMemberTypeRequest request, CancellationToken cancellationToken);
    
    Task<DeleteMemberTypeResult> DeleteAsync(Guid id, CancellationToken cancellationToken);
    
    Task<MemberTypeDto?> SetDefaultAsync(Guid id, CancellationToken cancellationToken);
}

public enum DeleteMemberTypeResult { Success, NotFound, HasMembers }