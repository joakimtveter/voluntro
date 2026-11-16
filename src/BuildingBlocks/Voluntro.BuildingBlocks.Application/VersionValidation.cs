using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Application;

public static class VersionValidation
{
    /// <summary>The request field that carries the version the client read.</summary>
    public const string Field = "version";

    /// <summary>
    /// Adds <c>version: required</c> when an update carries no version. One line in every update validator:
    /// <c>errors.RequireVersion(instance.Version);</c>
    /// </summary>
    /// <remarks>
    /// A missing <c>version</c> binds as 0, and a saved aggregate's version is never 0: Postgres never assigns
    /// transaction ID 0 to a row's <c>xmin</c>. So 0 always means the client left it out. Unchecked, it would reach
    /// <see cref="AggregateVersionExtensions.EnsureVersion{TId}"/> and come back as a 409 that no reload can fix.
    /// </remarks>
    public static void RequireVersion(this ICollection<FieldError> errors, uint version)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (version == 0)
        {
            errors.Add(new FieldError(Field, FieldErrorCodes.Required));
        }
    }
}
