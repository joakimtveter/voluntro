namespace Voluntro.SharedKernel.Tests;

public sealed class ResultTests
{
    private static readonly Error NotFound = Error.NotFound("test.thing.not_found");

    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_carries_its_error()
    {
        var result = Result.Failure(NotFound);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(NotFound, result.Error);
    }

    [Fact]
    public void Failure_requires_an_error()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => Result.Failure<string>(null!));
    }

    [Fact]
    public void An_error_converts_implicitly_to_a_failed_result()
    {
        Result result = NotFound;

        Assert.True(result.IsFailure);
        Assert.Equal(NotFound, result.Error);
    }

    [Fact]
    public void Success_with_a_value_exposes_the_value()
    {
        var result = Result.Success("Kari");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal("Kari", result.Value);
    }

    [Fact]
    public void Success_works_with_value_types()
    {
        var result = Result.Success(42);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Success_rejects_a_null_value()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success<string>(null!));
    }

    [Fact]
    public void Reading_the_value_of_a_failed_result_throws()
    {
        var result = Result.Failure<string>(NotFound);

        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);

        Assert.Contains(NotFound.Code, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_value_converts_implicitly_to_a_successful_result()
    {
        Result<string> result = "Kari";

        Assert.True(result.IsSuccess);
        Assert.Equal("Kari", result.Value);
    }

    [Fact]
    public void A_null_value_does_not_convert_to_a_successful_result()
    {
        string value = null!;

        Assert.Throws<ArgumentNullException>(() =>
        {
            Result<string> result = value;
            return result;
        });
    }

    [Fact]
    public void An_error_converts_implicitly_to_a_failed_result_with_a_value()
    {
        Result<string> result = NotFound;

        Assert.True(result.IsFailure);
        Assert.Equal(NotFound, result.Error);
    }

    [Fact]
    public void Handlers_can_return_values_and_errors_directly()
    {
        static Result<string> Find(bool exists) => exists ? "Kari" : NotFound;

        Assert.Equal("Kari", Find(exists: true).Value);
        Assert.Equal(NotFound, Find(exists: false).Error);
    }

    [Fact]
    public void A_result_with_a_value_is_a_result()
    {
        // Lets decorators constrain `where TResponse : Result` and inspect any handler's outcome.
        static bool Succeeded<TResponse>(TResponse response)
            where TResponse : Result => response.IsSuccess;

        Assert.True(Succeeded(Result.Success("Kari")));
        Assert.False(Succeeded(Result.Failure<string>(NotFound)));
    }

    [Fact]
    public void The_compiler_knows_error_is_not_null_after_checking_for_failure()
    {
        // These lines only compile without nullable warnings (= errors here) because
        // IsFailure and IsSuccess carry [MemberNotNullWhen].
        Result result = NotFound;

        if (result.IsFailure)
        {
            Assert.Equal(NotFound.Code, result.Error.Code);
        }

        if (!result.IsSuccess)
        {
            Assert.Equal(NotFound.Code, result.Error.Code);
        }
    }
}
