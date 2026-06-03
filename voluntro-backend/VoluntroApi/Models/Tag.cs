using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Models;

public class Tag
{
    public Guid Id { get; init; }
    
    [Required]
    [MaxLength(100)] 
    public required string Name { get; set; }
    
    [Required]
    [MaxLength(7)]
    public required string Color { get; set; }
    
    public ICollection<MemberTag> MemberTags { get; set; } = [];
}