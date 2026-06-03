namespace VoluntroApi.Models;

public class MemberTag
{
    public Guid MemberId { get; init; }

    public Guid TagId { get; init; }
    
    public Member Member { get; set; } = null!;
    
    public Tag Tag { get; set; } = null!;
}