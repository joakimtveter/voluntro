using Microsoft.EntityFrameworkCore;
using VoluntroApi.Data;
using VoluntroApi.Dtos.Shared;

namespace VoluntroApi.Services;

/// <inheritdoc />
public class QueryService(AppDbContext db, ILogger<QueryService> logger) : IQueryService
{
    /// <inheritdoc />
    public async Task<List<SelectOptionDto>> GetVenueOptions(string? query, CancellationToken cancellationToken)
    {
        if (query is null)
        {
            return await db.Venues
                .AsNoTracking()
                .OrderBy(v => v.Name)
                .Take(6)
                .Select(v => new SelectOptionDto
                {
                    Id = v.Id,
                    Label = v.Name,
                })
                .ToListAsync(cancellationToken);
        }
        
        return await db.Venues
            .AsNoTracking()
            .Where(v => v.Name.Contains(query))
            .OrderBy(v => v.Name)
            .Take(6)
            .Select(v => new SelectOptionDto
            {
                Id =  v.Id,
                Label = v.Name,
            })
            .ToListAsync(cancellationToken);
        
    }

    /// <inheritdoc />
    public async Task<List<SelectOptionDto>> GetGroupOptions(string? query, CancellationToken cancellationToken)
    {
        if (query is null)
        {
            return await db.Groups
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .Take(6)
                .Select(g => new SelectOptionDto
                {
                    Id = g.Id,
                    Label = g.Name,
                })
                .ToListAsync(cancellationToken);
        }
        
        return await db.Groups
            .AsNoTracking()
            .Where(g => g.Name.Contains(query))
            .OrderBy(g => g.Name)
            .Take(6)
            .Select(g => new SelectOptionDto
            {
                Id =  g.Id,
                Label = g.Name,
            })
            .ToListAsync(cancellationToken);

    }
}