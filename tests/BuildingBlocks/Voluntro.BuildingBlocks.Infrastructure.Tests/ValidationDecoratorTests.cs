using Voluntro.BuildingBlocks.Application;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// The validation decorator returns the validator's field errors as a <see cref="ValidationError"/> and never calls
/// what it wraps when there are any.
/// </summary>
public sealed class ValidationDecoratorTests
{
    private static readonly FieldError NameRequired = new("name", FieldErrorCodes.Required);

    [Fact]
    public async Task Valid_input_reaches_the_handler()
    {
        var handler = new Handler();
        var decorator = new ValidationDecorator<string, int>(handler, new Validator([]));

        var result = await decorator.HandleAsync("go", TestContext.Current.CancellationToken);

        Assert.Equal(42, result.Value);
        Assert.True(handler.Called);
    }

    [Fact]
    public async Task Invalid_input_returns_the_field_errors_and_never_reaches_the_handler()
    {
        var handler = new Handler();
        var decorator = new ValidationDecorator<string, int>(handler, new Validator([NameRequired]));

        var result = await decorator.HandleAsync("go", TestContext.Current.CancellationToken);

        Assert.Equal(new ValidationError([NameRequired]), result.Error);
        Assert.False(handler.Called);
    }

    [Fact]
    public async Task Without_a_value_valid_input_reaches_the_handler()
    {
        var handler = new HandlerWithoutValue();
        var decorator = new ValidationDecorator<string>(handler, new Validator([]));

        var result = await decorator.HandleAsync("go", TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(handler.Called);
    }

    [Fact]
    public async Task Without_a_value_invalid_input_returns_the_field_errors_and_never_reaches_the_handler()
    {
        var handler = new HandlerWithoutValue();
        var decorator = new ValidationDecorator<string>(handler, new Validator([NameRequired]));

        var result = await decorator.HandleAsync("go", TestContext.Current.CancellationToken);

        Assert.Equal(new ValidationError([NameRequired]), result.Error);
        Assert.False(handler.Called);
    }

    private sealed class Validator(IReadOnlyList<FieldError> errors) : IValidator<string>
    {
        public IReadOnlyList<FieldError> Validate(string instance) => errors;
    }

    private sealed class Handler : ICommandHandler<string, int>
    {
        public bool Called { get; private set; }

        public Task<Result<int>> HandleAsync(string command, CancellationToken cancellationToken)
        {
            Called = true;
            return Task.FromResult<Result<int>>(42);
        }
    }

    private sealed class HandlerWithoutValue : ICommandHandler<string>
    {
        public bool Called { get; private set; }

        public Task<Result> HandleAsync(string command, CancellationToken cancellationToken)
        {
            Called = true;
            return Task.FromResult(Result.Success());
        }
    }
}
