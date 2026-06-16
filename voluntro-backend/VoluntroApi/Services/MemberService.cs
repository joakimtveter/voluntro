using Microsoft.EntityFrameworkCore;
using VoluntroApi.Data;
using VoluntroApi.Dtos.Members;
using VoluntroApi.Dtos.Members.Address;
using VoluntroApi.Dtos.Members.PhoneNumber;
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
            (MemberSortBy.CreatedAt, SortDirection.Asc) => baseQuery.OrderBy(m => m.CreatedAt),
            (MemberSortBy.CreatedAt, SortDirection.Desc) => baseQuery.OrderByDescending(m => m.CreatedAt),
            (MemberSortBy.UpdatedAt, SortDirection.Asc) => baseQuery.OrderBy(m => m.UpdatedAt),
            (MemberSortBy.UpdatedAt, SortDirection.Desc) => baseQuery.OrderByDescending(m => m.UpdatedAt),
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
            .Include(m => m.PhoneNumbers)
            .Include(m => m.Addresses)
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
        await db.MemberTags.Where(mt => mt.MemberId == memberId).ExecuteDeleteAsync(cancellationToken);
        await db.MemberPhoneNumbers.Where(mp => mp.MemberId == memberId).ExecuteDeleteAsync(cancellationToken);
        await db.MemberAddresses.Where(ma => ma.MemberId == memberId).ExecuteDeleteAsync(cancellationToken);
        
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

    /// <inheritdoc/>
    public async Task<MemberPhoneNumberDto?> AddPhoneNumberAsync(Guid memberId, CreateMemberPhoneNumberRequest request, CancellationToken cancellationToken)
    {
        var memberExist = await db.Members.AnyAsync(m => m.Id == memberId && !m.IsDeleted, cancellationToken);
        if (!memberExist)
        {
            logger.LogWarning("AddPhoneNumber failed — member not found MemberId={MemberId}", memberId);
            return null;
        }

        var memberPhoneNumber = new MemberPhoneNumber
        {
            MemberId = memberId,
            PhoneType = request.PhoneType,
            CountryCode = request.CountryCode,
            PhoneNumber = request.PhoneNumber,
            IsPrimary = request.IsPrimary,
            CanReceiveTexts = request.CanReceiveTexts,
        };
            
        db.MemberPhoneNumbers.Add(memberPhoneNumber);
        await db.SaveChangesAsync(cancellationToken);
    
        logger.LogInformation("MemberPhoneNumber added MemberId={MemberId} PhoneNumber={PhoneNumber}", memberId, memberPhoneNumber.PhoneNumber);
        
        return new MemberPhoneNumberDto
        {
            Id = memberPhoneNumber.Id, 
            PhoneType = memberPhoneNumber.PhoneType, 
            CountryCode = memberPhoneNumber.CountryCode, 
            PhoneNumber = memberPhoneNumber.PhoneNumber, 
            IsPrimary = memberPhoneNumber.IsPrimary, 
            CanReceiveTexts = memberPhoneNumber.CanReceiveTexts
        };
    }
    
    /// <inheritdoc/>
    public async Task<MemberPhoneNumberDto?> UpdatePhoneNumberAsync(Guid memberId, Guid phoneNumberId, UpdateMemberPhoneNumberRequest request,
        CancellationToken cancellationToken)
    {
        var memberExist = await db.Members.AnyAsync(m => m.Id == memberId && !m.IsDeleted, cancellationToken);
        if (!memberExist)
        {
            logger.LogWarning("UpdatePhoneNumber failed — member not found MemberId={MemberId}", memberId);
            return null;
        }
        var phoneNumber = await db.MemberPhoneNumbers.FirstOrDefaultAsync(p => p.Id == phoneNumberId && p.MemberId == memberId, cancellationToken);
        if (phoneNumber is null)
        {
            logger.LogWarning("UpdatePhoneNumber failed — No record found for MemberId={MemberId} and PhoneNumberId={PhoneNumberId}", memberId, phoneNumberId);
            return null;
        }
        
        phoneNumber.PhoneType = request.PhoneType;
        phoneNumber.CountryCode = request.CountryCode;
        phoneNumber.PhoneNumber = request.PhoneNumber;
        phoneNumber.IsPrimary = request.IsPrimary;
        phoneNumber.CanReceiveTexts = request.CanReceiveTexts;
    
        await db.SaveChangesAsync(cancellationToken);

        return new MemberPhoneNumberDto
        {
            Id = phoneNumber.Id,
            PhoneType = phoneNumber.PhoneType,
            CountryCode = phoneNumber.CountryCode,
            PhoneNumber = phoneNumber.PhoneNumber,
            IsPrimary = phoneNumber.IsPrimary,
            CanReceiveTexts = phoneNumber.CanReceiveTexts
        };
    }
    
    /// <inheritdoc/>
    public async Task<bool?> DeletePhoneNumberAsync(Guid memberId, Guid phoneNumberId, CancellationToken cancellationToken)
    {
        var memberExist = await db.Members.AnyAsync(m => m.Id == memberId && !m.IsDeleted, cancellationToken);

        if (!memberExist)
        {
            logger.LogWarning("Delete phone number failed — member not found MemberId={MemberId}", memberId);
            return null;
        }
        
        var count = await db.MemberPhoneNumbers
            .Where(p => p.Id == phoneNumberId && p.MemberId == memberId)
            .ExecuteDeleteAsync(cancellationToken);

        if (count <= 0)
        {
            logger.LogWarning("Delete phone number failed - No Record found.");
            return false;
        }
        
        logger.LogInformation("MemberPhoneNumber deleted MemberId={MemberId} PhoneNumberId={PhoneNumberId}", memberId, phoneNumberId);
        return true;
    }
    
    /// <inheritdoc/>
    public async Task<MemberAddressDto?> AddAddressAsync(Guid memberId, CreateMemberAddressRequest request, CancellationToken cancellationToken)
    {
        var memberExist = await db.Members.AnyAsync(m => m.Id == memberId && !m.IsDeleted, cancellationToken);
        if (!memberExist)
        {
            logger.LogWarning("Add address failed — member not found MemberId={MemberId}", memberId);
            return null;
        }

        var memberAddress = new MemberAddress 
        {
            MemberId = memberId,
            Name = request.Name,
            StreetAddress = request.StreetAddress,
            StreetAddress2 = request.StreetAddress2,
            PostalCode =  request.PostalCode,
            City = request.City,
            Country = request.Country,
            IsPostalAddress = request.IsPostalAddress,
            IsVisitingAddress = request.IsVisitingAddress
        };
            
        db.MemberAddresses.Add(memberAddress);
        await db.SaveChangesAsync(cancellationToken);
    
        logger.LogInformation("Member address added to MemberId={MemberId}.", memberId);
        
        return new MemberAddressDto
        {
            Id = memberAddress.Id, 
            Name = memberAddress.Name,
            StreetAddress = memberAddress.StreetAddress,
            StreetAddress2 = memberAddress.StreetAddress2,
            PostalCode =  memberAddress.PostalCode,
            City = memberAddress.City,
            Country = memberAddress.Country,
            IsPostalAddress = memberAddress.IsPostalAddress,
            IsVisitingAddress = memberAddress.IsVisitingAddress
        };
    }

    /// <inheritdoc/>
    public async Task<MemberAddressDto?> UpdateAddressAsync(Guid memberId, Guid addressId,
        UpdateMemberAddressRequest request, CancellationToken cancellationToken)
    {
        var memberExist = await db.Members.AnyAsync(m => m.Id == memberId && !m.IsDeleted, cancellationToken);
        if (!memberExist)
        {
            logger.LogWarning("UpdateAddress failed — member not found MemberId={MemberId}", memberId);
            return null;
        }
        var address = await db.MemberAddresses.FirstOrDefaultAsync(a => a.Id == addressId && a.MemberId == memberId, cancellationToken);
        if (address is null)
        {
            logger.LogWarning("UpdateAddress failed — No record found for MemberId={MemberId} and AddressId={AddressId}", memberId, addressId);
            return null;
        }
        
        address.Name = request.Name;
        address.StreetAddress = request.StreetAddress;
        address.StreetAddress2 = request.StreetAddress2;
        address.PostalCode = request.PostalCode;
        address.City = request.City;
        address.Country = request.Country;
        address.IsPostalAddress = request.IsPostalAddress;
        address.IsVisitingAddress = request.IsVisitingAddress;
        
        await db.SaveChangesAsync(cancellationToken);

        return new MemberAddressDto
        {
            Id = address.Id,
            Name = address.Name,
            StreetAddress = address.StreetAddress,
            StreetAddress2 = address.StreetAddress2,
            PostalCode = address.PostalCode,
            City = address.City,
            Country = address.Country,
            IsPostalAddress = address.IsPostalAddress,
            IsVisitingAddress = address.IsVisitingAddress,
        };
    }

    public async Task<bool?> DeleteAddressAsync(Guid memberId, Guid addressId, CancellationToken cancellationToken)
    {
        var memberExist = await db.Members.AnyAsync(m => m.Id == memberId && !m.IsDeleted, cancellationToken);

        if (!memberExist)
        {
            logger.LogWarning("Delete address failed — member not found MemberId={MemberId}", memberId);
            return null;
        }
        
        var count = await db.MemberAddresses
            .Where(a => a.Id == addressId && a.MemberId == memberId)
            .ExecuteDeleteAsync(cancellationToken);

        if (count <= 0)
        {
            logger.LogWarning("Delete address failed - No Record found.");
            return false;
        }
        
        logger.LogInformation("Member address deleted MemberId={MemberId} AddressId={AddressId}", memberId, addressId);
        return true;
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
        PhoneNumbers = m.PhoneNumbers.Select(p => new MemberPhoneNumberDto
        {
            Id = p.Id,
            PhoneType = p.PhoneType,
            CountryCode = p.CountryCode,
            PhoneNumber = p.PhoneNumber,
            IsPrimary = p.IsPrimary,
            CanReceiveTexts = p.CanReceiveTexts,
        }).ToList(),
        Addresses = m.Addresses.Select(a => new MemberAddressDto
        {
            Id = a.Id,
            Name = a.Name,
            StreetAddress = a.StreetAddress,
            StreetAddress2 = a.StreetAddress2,
            PostalCode = a.PostalCode,
            City = a.City,
            Country = a.Country,
            IsPostalAddress = a.IsPostalAddress,
            IsVisitingAddress = a.IsVisitingAddress,
        }).ToList(),
        LegalGender = m.LegalGender,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt,
        IsDeleted = m.IsDeleted,
        MemberTypeId = m.MemberTypeId,
    };
}
