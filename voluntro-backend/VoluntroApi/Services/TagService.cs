using Microsoft.EntityFrameworkCore;
using VoluntroApi.Data;
using VoluntroApi.Dtos.Tags;
using VoluntroApi.Models;

namespace VoluntroApi.Services;

public class TagService(AppDbContext db, ILogger<VenueService> logger) : ITagService
{
    public async Task<List<Tag>> GetAllAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting all tags");
        
        var tags = await db.Tags.ToListAsync(cancellationToken);
        
        logger.LogDebug("Found {Count} tags", tags.Count);
        
        return tags;
    }

    public async Task<TagDto> CreateAsync(CreateTagRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var tag = new Tag
        {
            Name = request.Name.Trim(),
            Color = request.Color.Trim(),
        };

        db.Tags.Add(tag);
        await db.SaveChangesAsync(cancellationToken);

        return new TagDto { Id = tag.Id, Name = tag.Name, Color = tag.Color };
    }

    public async Task<bool?> DeleteAsync(Guid tagId, CancellationToken cancellationToken)
    {
        var count = await db.Tags.Where(t => t.Id == tagId).ExecuteDeleteAsync(cancellationToken);
        return count > 0 ? true : null;
    }
}