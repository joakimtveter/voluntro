namespace Voluntro.SharedKernel.Tests;

public sealed class ErrorTests
{
    [Fact]
    public void Constructor_sets_code_type_and_detail()
    {
        var error = new Error("membership.person.not_found", ErrorType.NotFound, "No person with that ID.");

        Assert.Equal("membership.person.not_found", error.Code);
        Assert.Equal(ErrorType.NotFound, error.Type);
        Assert.Equal("No person with that ID.", error.Detail);
    }

    [Fact]
    public void Detail_is_optional()
    {
        var error = new Error("membership.person.not_found", ErrorType.NotFound);

        Assert.Null(error.Detail);
    }

    [Theory]
    [InlineData("membership.person.not_found")]
    [InlineData("common.concurrency_conflict")]
    [InlineData("a.b")]
    [InlineData("identity.user2.totp_v2_invalid")]
    public void Well_formed_codes_are_accepted(string code)
    {
        var error = new Error(code, ErrorType.Failure);

        Assert.Equal(code, error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("membership")]
    [InlineData("Membership.person.not_found")]
    [InlineData("membership.Person.not_found")]
    [InlineData("membership..person")]
    [InlineData(".membership.person")]
    [InlineData("membership.person.")]
    [InlineData("membership.person-not-found")]
    [InlineData("membership.1person")]
    [InlineData("membership._person")]
    [InlineData("membership.person not_found")]
    [InlineData(" membership.person")]
    [InlineData("membership.person\n")]
    public void Malformed_codes_are_rejected(string code)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() => new Error(code, ErrorType.Failure));

        Assert.Equal("code", exception.ParamName);
    }

    [Fact]
    public void A_null_code_is_rejected()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new Error(null!, ErrorType.Failure));

        Assert.Equal("code", exception.ParamName);
    }

    [Fact]
    public void An_undefined_error_type_is_rejected()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Error("a.b", (ErrorType)42));

        Assert.Equal("type", exception.ParamName);
    }

    [Fact]
    public void The_default_error_type_is_failure()
    {
        Assert.Equal(ErrorType.Failure, default);
    }

    public static TheoryData<Func<string, string?, Error>, ErrorType> Factories => new()
    {
        { Error.Failure, ErrorType.Failure },
        { Error.Validation, ErrorType.Validation },
        { Error.NotFound, ErrorType.NotFound },
        { Error.Conflict, ErrorType.Conflict },
        { Error.Forbidden, ErrorType.Forbidden },
        { Error.Unauthorized, ErrorType.Unauthorized },
    };

    [Theory]
    [MemberData(nameof(Factories))]
    public void Factories_create_an_error_of_their_type(Func<string, string?, Error> factory, ErrorType expectedType)
    {
        var error = factory("module.thing.went_wrong", "Some detail.");

        Assert.Equal(new Error("module.thing.went_wrong", expectedType, "Some detail."), error);
    }

    [Fact]
    public void Factories_validate_the_code()
    {
        Assert.ThrowsAny<ArgumentException>(() => Error.NotFound("not a code"));
    }

    [Fact]
    public void Errors_with_the_same_code_type_and_detail_are_equal()
    {
        Assert.Equal(Error.NotFound("membership.person.not_found"), Error.NotFound("membership.person.not_found"));
    }

    [Fact]
    public void Errors_that_differ_in_any_part_are_not_equal()
    {
        var error = Error.NotFound("membership.person.not_found");

        Assert.NotEqual(error, Error.NotFound("membership.group.not_found"));
        Assert.NotEqual(error, Error.Conflict("membership.person.not_found"));
        Assert.NotEqual(error, Error.NotFound("membership.person.not_found", "Detail."));
    }

    [Fact]
    public void A_detail_can_be_added_to_a_catalog_error()
    {
        var catalogError = Error.NotFound("membership.person.not_found");

        var withDetail = catalogError with { Detail = "Looked up by email." };

        Assert.Equal(catalogError.Code, withDetail.Code);
        Assert.Equal(catalogError.Type, withDetail.Type);
        Assert.Equal("Looked up by email.", withDetail.Detail);
        Assert.Null(catalogError.Detail);
    }
}
