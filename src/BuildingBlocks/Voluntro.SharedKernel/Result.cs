using System.Diagnostics.CodeAnalysis;

namespace Voluntro.SharedKernel;

/// <summary>
/// The outcome of an operation that can fail in an expected way: success, or a single <see cref="Error"/>.
/// Exceptions are reserved for bugs and infrastructure faults (ADR-0003).
/// </summary>
/// <remarks>
/// Factories live here rather than on <see cref="Result{T}"/> because CA1000 forbids static
/// members on generic types. Handlers can also <c>return value;</c> or <c>return SomeErrors.X;</c>
/// through the implicit conversions.
/// </remarks>
public class Result
{
    private static readonly Result SuccessResult = new(null);

    private protected Result(Error? error) => Error = error;

    /// <summary>The failure's error; <see langword="null"/> on success.</summary>
    public Error? Error { get; }

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => !IsFailure;

    public static Result Success() => SuccessResult;

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(error);
    }

    public static Result<T> Success<T>(T value)
        where T : notnull => new(value);

    public static Result<T> Failure<T>(Error error)
        where T : notnull => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>A <see cref="Result"/> that carries a value on success.</summary>
/// <typeparam name="T">The value's type. Not nullable: a missing thing is a NotFound error.</typeparam>
public sealed class Result<T> : Result
    where T : notnull
{
    private readonly T? _value;

    internal Result(T value)
        : base(null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), "A successful result needs a value; return a NotFound error instead.");
        }

        _value = value;
    }

    internal Result(Error error)
        : base(error ?? throw new ArgumentNullException(nameof(error)))
    {
    }

    /// <summary>The value of a successful result.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure. Check <see cref="Result.IsSuccess"/> first.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({Error.Code}). Check IsSuccess first.");

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);
}
