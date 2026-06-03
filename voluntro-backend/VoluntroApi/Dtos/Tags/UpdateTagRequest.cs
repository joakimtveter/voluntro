using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Dtos.Tags;

public class UpdateTagRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(7)]
    public string Color { get; init; } = string.Empty;
}