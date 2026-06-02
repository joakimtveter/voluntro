namespace VoluntroApi.Dtos.Members;

/// <summary>
/// Represents a member returned from the API.
/// </summary>
public class MemberBriefDto
{
    /// <summary>
    /// Unique identifier for the member.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// First name of the member.
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// Middle name(s) of the member, if any.
    /// </summary>
    public string? MiddleNames { get; init; }

    /// <summary>
    /// Last name of the member.
    /// </summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Date of birth of the member, if known.
    /// </summary>
    public DateOnly? DateOfBirth { get; init; }

    /// <summary>
    /// Age in years derived from DateOfBirth, or null if DateOfBirth is unknown.
    /// </summary>
    public int? Age => DateOfBirth.HasValue ? CalculateAge(DateOfBirth.Value) : null;

    /// <summary>
    /// Gender of the member.
    /// </summary>
    public LegalGender LegalGender { get; init; }

    /// <summary>
    /// UTC timestamp when the member was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// UTC timestamp when the member was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Indicates whether the member has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; init; }

    private static int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age)) age--;
        return age;
    }
}

/// <inheritdoc />
public class MemberDto : MemberBriefDto
{

}