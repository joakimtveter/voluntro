using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.GroupMemberships;

public class UpdateGroupMembershipRequest
{
    [Required]
    public required Guid GroupId { get; init; } 
    
    [Required]
    public required Guid MemberId { get; init; }
}