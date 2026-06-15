using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.MemberTypes;

public class UpdateMemberTypeRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;
}