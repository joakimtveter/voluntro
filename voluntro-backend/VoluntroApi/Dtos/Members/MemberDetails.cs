namespace VoluntroApi.Dtos.Members;

/// <inheritdoc />
public class MemberDetails : MemberSummary
{
    /// <summary>
    /// Age in years derived from DateOfBirth, or null if DateOfBirth is unknown.
    /// </summary>
    public int? Age => DateOfBirth.HasValue ? CalculateAge(DateOfBirth.Value) : null;
    
    private static int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age)) age--;
        return age;
    }
}