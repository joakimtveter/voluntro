using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.MemberTypes;

public class CreateMemberTypeRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;
}