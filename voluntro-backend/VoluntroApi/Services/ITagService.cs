using VoluntroApi.Dtos.Tags;
using VoluntroApi.Models;

namespace VoluntroApi.Services;

public interface ITagService
{
 Task<List<Tag>> GetAllAsync(CancellationToken cancellationToken);
 Task<TagDto> CreateAsync(CreateTagRequest request, CancellationToken cancellationToken);
 Task<TagDto?> UpdateAsync(Guid tagId, UpdateTagRequest request, CancellationToken cancellationToken);
 Task<bool?> DeleteAsync(Guid id, CancellationToken cancellationToken);
}