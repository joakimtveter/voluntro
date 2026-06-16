using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.Members.Address;

public class CreateMemberAddressRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(250)]
    public string StreetAddress { get; set; } = string.Empty;
    
    [MaxLength(250)]
    public string? StreetAddress2 { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;
    
    public bool IsPostalAddress { get; init; }
    
    public bool IsVisitingAddress { get; init; }
}