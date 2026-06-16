using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Models;

public class MemberAddress
{
    public Guid Id { get; init; }
    
    public Guid MemberId { get; set; }
    
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
    
    public bool IsPostalAddress { get; set; }
    
    public bool IsVisitingAddress { get; set; }
}