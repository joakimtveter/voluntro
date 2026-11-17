using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Application;

/// <summary>
/// Checks a command's shape before its handler runs: required fields, lengths, ranges, formats. Synchronous and
/// never touches the database; a rule like "email already taken" belongs to the handler or the domain and is a
/// Conflict. Registered with the handler through the module's command registrar (<c>AddValidated</c>).
/// </summary>
/// <typeparam name="T">The command.</typeparam>
public interface IValidator<in T>
{
    /// <summary>Every problem found, in field order; empty when the command is valid.</summary>
    IReadOnlyList<FieldError> Validate(T instance);
}
