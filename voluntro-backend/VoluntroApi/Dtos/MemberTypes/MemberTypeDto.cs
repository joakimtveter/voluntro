namespace VoluntroApi.Dtos.MemberTypes;

public class MemberTypeDto
{
    public Guid Id { get; init; }
    
    public string Name { get; init; } = string.Empty;
    
    public bool IsDefault { get; init; } = false;
}