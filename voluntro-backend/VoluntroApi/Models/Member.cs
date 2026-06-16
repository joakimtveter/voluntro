using System.ComponentModel.DataAnnotations;
using VoluntroApi.Dtos.Members;

namespace VoluntroApi.Models;

/// <summary>
/// Represents a organization member
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
    /// Email address of the member
    /// </summary>
    [MaxLength(254)]
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// Date of birth of the member
    /// </summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>
    /// Gender of the member
    /// </summary>
    public LegalGender LegalGender {get; set;}
    
    public ICollection<MemberPhoneNumber> PhoneNumbers { get; set; } = [];
    
    public ICollection<MemberAddress> Addresses { get; set; } = [];
    
    /// <summary>
    /// Gender of the member
    /// </summary>
    public Guid MemberTypeId { get; set; }
    
    public MemberType MemberType { get; set; } = null!;
    
    /// <summary>
    /// List of tags
    /// </summary>
    public ICollection<MemberTag> MemberTags { get; set; } = new List<MemberTag>();

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

    public static string FormatFullName(string firstName, string? middleNames, string lastName) =>
        string.IsNullOrWhiteSpace(middleNames)
            ? $"{firstName} {lastName}"
            : $"{firstName} {middleNames} {lastName}";

    public string FullName => FormatFullName(FirstName, MiddleNames, LastName);
}