namespace VoluntroApi.Dtos.Members.Address;

public class MemberAddressDto
{    
    public Guid Id { get; init; }
    
    public string Name { get; set; } = string.Empty;
    
    public string StreetAddress { get; set; } = string.Empty;
    
    public string? StreetAddress2 { get; set; }
    
    public string PostalCode { get; set; } = string.Empty;
    
    public string City { get; set; } = string.Empty;
    
    public string Country { get; set; } = string.Empty;
    
    public bool IsPostalAddress { get; set; }
    
    public bool IsVisitingAddress { get; set; }
    
}