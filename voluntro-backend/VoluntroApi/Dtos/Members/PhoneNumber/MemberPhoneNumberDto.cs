namespace VoluntroApi.Dtos.Members.PhoneNumber;

public class MemberPhoneNumberDto
{
    public Guid Id { get; init; }
    
    public string PhoneType { get; set; } = string.Empty;
    
    public string CountryCode { get; set; } = string.Empty;
    
    public string PhoneNumber { get; set; } = string.Empty;
    
    public bool IsPrimary { get; set; } = false;
    
    public bool CanReceiveTexts { get; set; } = false;

}