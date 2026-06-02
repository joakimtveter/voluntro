namespace VoluntroApi.Dtos.Shared;

/// <summary>
/// A reusable DTO for use in lists.
/// </summary>
public class MinimalReference
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Display name.</summary>
    public string Name { get; init; } = string.Empty;
}