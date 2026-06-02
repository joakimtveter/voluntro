using Microsoft.EntityFrameworkCore;
using VoluntroApi.Data;
using VoluntroApi.Dtos.Shared;
using VoluntroApi.Dtos.Venues;
using VoluntroApi.Models;

namespace VoluntroApi.Services;

/// <summary>
/// EF Core-backed implementation of <see cref="IVenueService"/>.
/// </summary>
public class VenueService(AppDbContext db, ILogger<VenueService> logger) : IVenueService
{
    /// <inheritdoc />
    public async Task<PagedResult<VenueBriefDto>> GetAllAsync(GetVenuesQuery query, CancellationToken cancellationToken)
    {
        return await db.Venues
            .AsNoTracking()
            .Where(v => v.IsDeleted == false)
            .OrderBy(v => v.Name)
            .ToPagedResultAsync( v => new VenueBriefDto
            {
                Id = v.Id,
                Name = v.Name,
                UpdatedAt =  v.UpdatedAt,
                CreatedAt = v.CreatedAt,
                IsDeleted =  v.IsDeleted
            }, query.Page, query.PageSize, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<VenueDto?> GetByIdAsync(Guid venueId, CancellationToken cancellationToken, bool includeDeleted)
    {
        return await db.Venues
            .AsNoTracking()
            .Where(v => v.Id == venueId)
            .Where(v => includeDeleted || v.IsDeleted == false)
            .Select(v => new VenueDto
            {
                Id = v.Id,
                Name = v.Name,
                Description = v.Description,
                Address = new AddressDto
                {
                    StreetAddress = v.StreetAddress,
                    StreetAddress2 = v.StreetAddress2,
                    PostalCode = v.PostalCode,
                    City = v.City,
                    Country = v.Country
                },
                UpdatedAt = v.UpdatedAt,
                CreatedAt = v.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<VenueDto> CreateAsync(CreateVenueRequest request, CancellationToken cancellationToken)
    {
        var venue = new Venue()
        {
            Name = request.Name,
            Description = request.Description,
            StreetAddress = request.Address.StreetAddress,
            StreetAddress2 = request.Address.StreetAddress2,
            PostalCode = request.Address.PostalCode,
            City = request.Address.City,
            Country = request.Address.Country,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false
        };
        
        db.Venues.Add(venue);
        await db.SaveChangesAsync(cancellationToken);
        return new VenueDto
        {
            Id = venue.Id,
            Name = venue.Name,
            Description = venue.Description,
            Address = new AddressDto
            {
                StreetAddress = venue.StreetAddress,
                StreetAddress2 = venue.StreetAddress2,
                PostalCode = venue.PostalCode,
                City = venue.City,
                Country = venue.Country
            },
            CreatedAt = venue.CreatedAt,
            UpdatedAt = venue.UpdatedAt,
            IsDeleted = venue.IsDeleted
        };
    }

    /// <inheritdoc />
    public async Task<VenueDto?> UpdateAsync(Guid venueId, UpdateVenueRequest request, CancellationToken cancellationToken)
    {
        var venue = await db.Venues.FindAsync([venueId], cancellationToken);

        if (venue is null || venue.IsDeleted)
        {
            logger.LogWarning("Update failed — venue with Id={VenueId} not found", venueId);
            return null;
        }

        venue.Name = request.Name;
        venue.Description = request.Description;
        venue.StreetAddress = request.Address.StreetAddress;
        venue.StreetAddress2 = request.Address.StreetAddress2;
        venue.PostalCode = request.Address.PostalCode;
        venue.City = request.Address.City;
        venue.Country = request.Address.Country;
        venue.UpdatedAt = DateTimeOffset.UtcNow;
        
        await db.SaveChangesAsync(cancellationToken);
        return new VenueDto
        {
            Id = venue.Id,
            Name = venue.Name,
            Description = venue.Description,
            Address = new AddressDto
            {
                StreetAddress = venue.StreetAddress,
                StreetAddress2 = venue.StreetAddress2,
                PostalCode = venue.PostalCode,
                City = venue.City,
                Country = venue.Country
            }, 
            UpdatedAt = venue.UpdatedAt,
            CreatedAt = venue.CreatedAt,
            IsDeleted =  venue.IsDeleted
        };
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid venueId, CancellationToken cancellationToken)
    {
        var venueToDelete = await db.Venues.FindAsync([venueId], cancellationToken);

        if (venueToDelete is null || venueToDelete.IsDeleted)
        {
            logger.LogWarning("Delete failed — venue with Id={VenueId} not found", venueId);
            return false;
        }

        venueToDelete.IsDeleted = true;
        venueToDelete.UpdatedAt = DateTimeOffset.UtcNow;
        
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}