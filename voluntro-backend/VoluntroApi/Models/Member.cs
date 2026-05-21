using System.ComponentModel.DataAnnotations;
using VoluntroApi.Dtos.Members;

namespace VoluntroApi.Models;

/// <summary>
/// Represents a church member
/// </summary>
public class Member
{
    /// <summary>
    /// Unique identifier for the member
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// First name of the member
    /// </summary>
    [Required]
    [MaxLength(250)]
    public string FirstName { get; set; } =  string.Empty;

    /// <summary>
    /// Optional middle name(s) of the member
    /// </summary>
    [MaxLength(250)]
    public string? MiddleNames {get; set;}

    /// <summary>
    /// Last name of the member
    /// </summary>
    [Required]
    [MaxLength(250)]
    public string LastName { get; set; } =  string.Empty;

    /// <summary>
    /// Date of birth of the member
    /// </summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>
    /// Gender of the member
    /// </summary>
    public LegalGender LegalGender {get; set;}

    /// <summary>
    /// UTC timestamp when the member was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// UTC timestamp when the member was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
    
    /// <summary>
    /// Indicates whether the member has been soft-deleted.
    /// </summary>
    [Required]
    public bool IsDeleted {get; set;}

}