using VoluntroApi.Dtos.Members;
using VoluntroApi.Dtos.Members.Address;
using VoluntroApi.Dtos.Members.PhoneNumber;
using VoluntroApi.Dtos.Shared;
using VoluntroApi.Dtos.Tags;
using MemberPhoneNumberDto = VoluntroApi.Dtos.Members.PhoneNumber.MemberPhoneNumberDto;

namespace VoluntroApi.Services;

/// <summary>
/// Defines the contract for member management operations.
/// </summary>
public interface IMemberService
{
    /// <summary>
    /// Returns a paginated list of members ordered by last name then first name by default.
    /// </summary>
    /// <param name="query">Pagination and filter parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <param name="includeDeleted">When true, soft-deleted members are included.</param>
    /// <returns>A paged result of members.</returns>
    Task<PagedResult<MemberSummary>> GetAllAsync(GetMembersQuery query, CancellationToken cancellationToken, bool includeDeleted = false);

    /// <summary>
    /// Returns a single member by their unique identifier.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <param name="includeDeleted">When true, soft-deleted members are included.</param>
    /// <returns>The member, or null if not found.</returns>
    Task<MemberDetails?> GetByIdAsync(Guid memberId, CancellationToken cancellationToken, bool includeDeleted = false);

    /// <summary>
    /// Creates a new member.
    /// </summary>
    /// <param name="request">The member details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created member.</returns>
    Task<MemberDetails> CreateAsync(CreateMemberRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing member.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="request">The updated member details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated member, or null if not found.</returns>
    Task<MemberDetails?> UpdateAsync(Guid memberId, UpdateMemberRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes a member by their unique identifier.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the member was deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid memberId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Permanently erases all personal data for a member (GDPR Article 17 right to erasure).
    /// The record is anonymized and soft-deleted; the ID is retained as a pseudonymous placeholder.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if erased, false if not found.</returns>
    Task<bool> GdprDeleteAsync(Guid memberId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Restores a soft-deleted member.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The restored member, or null if not found or not deleted.</returns>
    Task<MemberDetails?> RestoreAsync(Guid memberId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Assigns a tag to a member.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="tagId">The tag's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success, or the reason for failure.</returns>
    Task<AddTagResult> AddTagAsync(Guid memberId, Guid tagId, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a tag from a member.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="tagId">The tag's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success, or the reason for failure.</returns>
    Task<RemoveTagResult> RemoveTagAsync(Guid memberId, Guid tagId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a phone number to a member.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="request">The phone number details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created phone number, or null if the member was not found.</returns>
    Task<MemberPhoneNumberDto?> AddPhoneNumberAsync(Guid memberId, CreateMemberPhoneNumberRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing phone number on a member.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="phoneNumberId">The phone number's unique identifier.</param>
    /// <param name="request">The updated phone number details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated phone number, or null if the member or phone number was not found.</returns>
    Task<MemberPhoneNumberDto?> UpdatePhoneNumberAsync(Guid memberId, Guid phoneNumberId, UpdateMemberPhoneNumberRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a phone number from a member.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="phoneNumberId">The phone number's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if deleted, null if the member or phone number was not found.</returns>
    Task<bool?> DeletePhoneNumberAsync(Guid memberId, Guid phoneNumberId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds an address to a member.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="request">The address details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created address, or null if the member was not found.</returns>
    Task<MemberAddressDto?> AddAddressAsync(Guid memberId, CreateMemberAddressRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing address on a member.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="addressId">The address's unique identifier.</param>
    /// <param name="request">The updated address details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated address, or null if the member or address was not found.</returns>
    Task<MemberAddressDto?> UpdateAddressAsync(Guid memberId, Guid addressId, UpdateMemberAddressRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an address from a member.
    /// </summary>
    /// <param name="memberId">The member's unique identifier.</param>
    /// <param name="addressId">The address's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if deleted, null if the member or address was not found.</returns>
    Task<bool?> DeleteAddressAsync(Guid memberId, Guid addressId, CancellationToken cancellationToken);

}