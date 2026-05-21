using System.ComponentModel.DataAnnotations;

namespace VoluntroApi.Models;

/// <summary>
/// Represents a physical venue where church events can take place.
/// </summary>
public class Venue
{
    /// <summary>
    /// Unique identifier of the venue.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Display name of the venue. Maximum 100 characters.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name {get; set;} = string.Empty;

    /// <summary>
    /// Short description of the venue. Maximum 250 characters.
    /// </summary>
    [MaxLength(250)]
    public string Description {get; set;} = string.Empty;

    /// <summary>
    /// Primary street address line. Maximum 100 characters.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string StreetAddress {get; set;} = string.Empty;

    /// <summary>
    /// Secondary address line (e.g. suite or floor number). Maximum 100 characters.
    /// </summary>
    [MaxLength(100)]
    public string? StreetAddress2 {get; set;}

    /// <summary>
    /// Postal or ZIP code. Maximum 20 characters.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string PostalCode {get; set;} = string.Empty;

    /// <summary>
    /// City the venue is located in. Maximum 100 characters.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string City {get; set;} = string.Empty;

    /// <summary>
    /// Country the venue is located in. Maximum 60 characters.
    /// </summary>
    [MaxLength(60)]
    public string? Country {get; set;}

    /// <summary>
    /// UTC timestamp when the venue was created.
    /// </summary>
    [Required]
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// UTC timestamp when the venue was last updated.
    /// </summary>
    [Required]
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Indicates whether the venue has been soft-deleted.
    /// </summary>
    [Required]
    public bool IsDeleted {get; set;}
}