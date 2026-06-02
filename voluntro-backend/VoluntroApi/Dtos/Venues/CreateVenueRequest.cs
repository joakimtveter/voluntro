using System.ComponentModel.DataAnnotations;
using VoluntroApi.Dtos.Shared;

namespace VoluntroApi.Dtos.Venues;

/// <summary>
/// Request body for creating a new venue.
/// </summary>
public class CreateVenueRequest
{
    /// <summary>
    /// Display name of the venue. Maximum 100 characters.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name {get; set;} = string.Empty;

    /// <summary>
    /// Short description of the venue. Maximum 250 characters.
    /// </summary>
    [Required]
    [MaxLength(250)]
    public string Description {get; set;} = string.Empty;

    /// <summary>
    /// Physical address of the venue.
    /// </summary>
    [Required]
    public AddressDto Address { get; set; } = new AddressDto();
}