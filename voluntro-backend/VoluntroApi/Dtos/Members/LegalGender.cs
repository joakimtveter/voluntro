using System.Text.Json.Serialization;

namespace VoluntroApi.Dtos.Members;

/// <summary>
/// Gender of a member
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LegalGender
{
    /// <summary>
    /// Gender is not specified
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Male
    /// </summary>
    Male = 1,

    /// <summary>
    /// Female
    /// </summary>
    Female = 2
}
