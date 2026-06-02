using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.Groups;

public record AddMemberToGroupRequest
{
    [Required]
    public required Guid MemberId { get; init; }
}
