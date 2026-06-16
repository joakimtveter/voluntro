using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Models;

public class MemberPhoneNumber
{
    public Guid Id { get; init; }
    
    public Guid MemberId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string PhoneType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string CountryCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string PhoneNumber { get; set; } = string.Empty;
    
    public bool IsPrimary { get; set; } = false;
    
    public bool CanReceiveTexts { get; set; } = false;
}