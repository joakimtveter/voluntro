using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.Memberships;

public class UpdateMembershipRequest
{
    [Required]
    public required Guid GroupId { get; init; } 
    
    [Required]
    public required Guid MemberId { get; init; }
}