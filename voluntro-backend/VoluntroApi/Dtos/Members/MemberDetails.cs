using VoluntroApi.Dtos.Members.Address;
using VoluntroApi.Dtos.Members.PhoneNumber;

namespace VoluntroApi.Dtos.Members;

/// <inheritdoc />
public class MemberDetails : MemberSummary
{
    /// <summary>
    /// Age in years derived from DateOfBirth, or null if DateOfBirth is unknown.
    /// </summary>
    public int? Age => DateOfBirth.HasValue ? CalculateAge(DateOfBirth.Value) : null;
    
    /// <summary>
    /// List of addresses associated with the member.
    /// </summary>
    public List<MemberAddressDto> Addresses { get; set; } = [];
    
    /// <summary>
    /// List of phone numbers associated with the member.
    /// </summary>
    public List<MemberPhoneNumberDto> PhoneNumbers { get; set; } = [];
    
    private static int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age)) age--;
        return age;
    }
}