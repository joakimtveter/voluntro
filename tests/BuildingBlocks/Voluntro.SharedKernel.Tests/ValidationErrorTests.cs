namespace Voluntro.SharedKernel.Tests;

public sealed class ValidationErrorTests
{
    private static readonly FieldError NameRequired = new("customerName", FieldErrorCodes.Required);
    private static readonly FieldError NoteTooLong = new("note", FieldErrorCodes.TooLong);

    [Fact]
    public void Is_a_validation_error_with_the_common_code()
    {
        var error = new ValidationError([NameRequired]);

        Assert.Equal("common.validation_failed", error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal([NameRequired], error.Errors);
    }

    [Fact]
    public void Needs_at_least_one_field_error()
    {
        Assert.Throws<ArgumentException>(() => new ValidationError([]));
        Assert.Throws<ArgumentException>(() => new ValidationError([NameRequired, null!]));
        Assert.Throws<ArgumentNullException>(() => new ValidationError(null!));
    }

    [Fact]
    public void Keeps_its_own_copy_of_the_field_errors()
    {
        var errors = new List<FieldError> { NameRequired };
        var error = new ValidationError(errors);

        errors.Add(NoteTooLong);

        Assert.Equal([NameRequired], error.Errors);
    }

    [Fact]
    public void Equal_when_the_field_errors_are_equal_in_the_same_order()
    {
        var one = new ValidationError([NameRequired, NoteTooLong]);
        var same = new ValidationError([new("customerName", "required"), new("note", "too_long")]);
        var reordered = new ValidationError([NoteTooLong, NameRequired]);

        Assert.Equal(one, same);
        Assert.Equal(one.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(one, reordered);
    }

    [Fact]
    public void Never_equals_a_plain_error_with_the_same_code()
    {
        Error validation = new ValidationError([NameRequired]);
        var plain = Error.Validation(ValidationError.ErrorCode);

        Assert.NotEqual(plain, validation);
        Assert.NotEqual(validation, plain);
    }
}

public sealed class FieldErrorTests
{
    [Theory]
    [InlineData("required")]
    [InlineData("too_long")]
    [InlineData("v2_format")]
    public void Accepts_a_snake_case_code(string code)
    {
        var error = new FieldError("customerName", code);

        Assert.Equal("customerName", error.Field);
        Assert.Equal(code, error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Required")]
    [InlineData("sample.required")]
    [InlineData("too-long")]
    [InlineData("required\n")]
    public void Rejects_a_code_that_is_not_one_snake_case_word(string code)
    {
        Assert.Throws<ArgumentException>(() => new FieldError("customerName", code));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Needs_a_field(string field)
    {
        Assert.Throws<ArgumentException>(() => new FieldError(field, FieldErrorCodes.Required));
    }
}
