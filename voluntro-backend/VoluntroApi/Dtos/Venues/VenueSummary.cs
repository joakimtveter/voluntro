namespace VoluntroApi.Dtos.Venues;

public class VenueSummary
{
    /// <summary>
    /// Unique identifier of the venue.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Display name of the venue.
    /// </summary>
    public string Name {get; set;} = string.Empty;
    
    /// <summary>
    /// Indicates whether the venue has been soft-deleted.
    /// </summary>
    public bool IsDeleted {get; set;}
}