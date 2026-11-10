namespace Voluntro.SharedKernel;

/// <summary>
/// The kind of an expected failure; maps to an HTTP status at the API edge.
/// Values are explicit so reordering can't change behaviour. <see cref="Failure"/> is the zero
/// value, so an accidental <c>default</c> surfaces as a generic failure, not as a client error.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Forbidden = 4,
    Unauthorized = 5,
}
