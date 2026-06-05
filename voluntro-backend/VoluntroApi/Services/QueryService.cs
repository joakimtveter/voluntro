using Microsoft.EntityFrameworkCore;
using VoluntroApi.Data;
using VoluntroApi.Dtos.Shared;
using VoluntroApi.Models;

namespace VoluntroApi.Services;

/// <inheritdoc />
public class QueryService(AppDbContext db, ILogger<QueryService> logger) : IQueryService
{
    /// <inheritdoc />
    public async Task<List<SelectOption>> GetVenueOptions(string? query, CancellationToken cancellationToken)
    {
        var dbQuery = db.Venues.AsNoTracking().OrderBy(v => v.Name);
        var filtered = query is null ? dbQuery : dbQuery.Where(v => v.Name.Contains(query));

        return await filtered
            .Take(6)
            .Select(v => new SelectOption { Id = v.Id, Label = v.Name })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<SelectOption>> GetGroupOptions(string? query, CancellationToken cancellationToken)
    {
        var dbQuery = db.Groups.AsNoTracking().OrderBy(g => g.Name);
        var filtered = query is null ? dbQuery : dbQuery.Where(g => g.Name.Contains(query));

        return await filtered
            .Take(6)
            .Select(g => new SelectOption { Id = g.Id, Label = g.Name })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<SelectOption>> GetMemberOptions(string? query, CancellationToken cancellationToken)
    {
        var dbQuery = db.Members
            .AsNoTracking()
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName);

        var filtered = query is null
            ? dbQuery
            : dbQuery.Where(m => m.LastName.Contains(query) || m.FirstName.Contains(query) || (m.MiddleNames != null && m.MiddleNames.Contains(query)));

        var rows = await filtered
            .Take(6)
            .Select(m => new { m.Id, m.FirstName, m.MiddleNames, m.LastName })
            .ToListAsync(cancellationToken);

        return rows.Select(m => new SelectOption
        {
            Id = m.Id,
            Label = Member.FormatFullName(m.FirstName, m.MiddleNames, m.LastName),
        }).ToList();
    }
}