using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Voluntro.Api.Options;

/// <summary>
/// Installation-wide settings, bound from the "App" configuration section.
/// </summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>
    /// The address users reach Voluntro on, e.g. https://voluntro.example.org. Used to build links in emails.
    /// </summary>
    [Required]
    [Url]
    public string PublicBaseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Validates <see cref="AppOptions"/> with code generated at compile time from its data annotations.
/// </summary>
[OptionsValidator]
public sealed partial class AppOptionsValidator : IValidateOptions<AppOptions>;
