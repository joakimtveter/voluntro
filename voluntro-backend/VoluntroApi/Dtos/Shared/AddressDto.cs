namespace VoluntroApi.Dtos.Shared;

/// <summary>
/// Represents a postal address.
/// </summary>
public class AddressDto
{
    /// <summary>
    /// Primary street address line.
    /// </summary>
    public string StreetAddress { get; init; } = string.Empty;

    /// <summary>
    /// Secondary street address line (e.g. apartment, suite, unit).
    /// </summary>
    public string? StreetAddress2 { get; init; }

    /// <summary>
    /// Postal or ZIP code.
    /// </summary>
    public string PostalCode { get; init; } = string.Empty;

    /// <summary>
    /// City or locality name.
    /// </summary>
    public string City { get; init; }  = string.Empty;

    /// <summary>
    /// Country name.
    /// </summary>
    public string? Country { get; init; }
}