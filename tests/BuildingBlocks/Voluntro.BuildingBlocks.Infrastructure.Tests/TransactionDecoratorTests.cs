using Voluntro.BuildingBlocks.Application;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// The transaction decorator saves only after a successful handler, and discards the changes after any failure
/// (ADR-0012). The unit of work is a fake that records what was asked of it; the database path is covered by the
/// sample module's integration tests.
/// </summary>
public sealed class TransactionDecoratorTests
{
    private static readonly Error HandlerError = Error.Conflict("test.thing.refused");

    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task A_successful_command_is_saved_once_and_returns_the_handlers_value()
    {
        var decorator = new TransactionDecorator<string, int>(new Handler(_ => 42), _unitOfWork);

        var result = await decorator.HandleAsync("go", TestContext.Current.CancellationToken);

        Assert.Equal(42, result.Value);
        Assert.Equal(["save"], _unitOfWork.Calls);
    }

    [Fact]
    public async Task A_failed_command_is_discarded_and_never_saved()
    {
        var decorator = new TransactionDecorator<string, int>(new Handler(_ => HandlerError), _unitOfWork);

        var result = await decorator.HandleAsync("go", TestContext.Current.CancellationToken);

        Assert.Equal(HandlerError, result.Error);
        Assert.Equal(["discard"], _unitOfWork.Calls);
    }

    [Fact]
    public async Task A_conflict_while_saving_is_discarded_and_returned()
    {
        _unitOfWork.SaveResult = CommonErrors.ConcurrencyConflict;
        var decorator = new TransactionDecorator<string, int>(new Handler(_ => 42), _unitOfWork);

        var result = await decorator.HandleAsync("go", TestContext.Current.CancellationToken);

        Assert.Equal(CommonErrors.ConcurrencyConflict, result.Error);
        Assert.Equal(["save", "discard"], _unitOfWork.Calls);
    }

    [Fact]
    public async Task An_exception_propagates_without_saving_or_discarding()
    {
        var decorator = new TransactionDecorator<string, int>(
            new Handler(_ => throw new InvalidOperationException("bug")),
            _unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => decorator.HandleAsync("go", TestContext.Current.CancellationToken));
        Assert.Empty(_unitOfWork.Calls);
    }

    [Fact]
    public async Task Without_a_value_a_successful_command_is_saved()
    {
        var decorator = new TransactionDecorator<string>(new HandlerWithoutValue(Result.Success()), _unitOfWork);

        var result = await decorator.HandleAsync("go", TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(["save"], _unitOfWork.Calls);
    }

    [Fact]
    public async Task Without_a_value_a_failed_command_is_discarded_and_never_saved()
    {
        var decorator = new TransactionDecorator<string>(new HandlerWithoutValue(HandlerError), _unitOfWork);

        var result = await decorator.HandleAsync("go", TestContext.Current.CancellationToken);

        Assert.Equal(HandlerError, result.Error);
        Assert.Equal(["discard"], _unitOfWork.Calls);
    }

    [Fact]
    public async Task Without_a_value_a_conflict_while_saving_is_discarded_and_returned()
    {
        _unitOfWork.SaveResult = CommonErrors.ConcurrencyConflict;
        var decorator = new TransactionDecorator<string>(new HandlerWithoutValue(Result.Success()), _unitOfWork);

        var result = await decorator.HandleAsync("go", TestContext.Current.CancellationToken);

        Assert.Equal(CommonErrors.ConcurrencyConflict, result.Error);
        Assert.Equal(["save", "discard"], _unitOfWork.Calls);
    }

    private sealed class Handler(Func<string, Result<int>> handle) : ICommandHandler<string, int>
    {
        public Task<Result<int>> HandleAsync(string command, CancellationToken cancellationToken) =>
            Task.FromResult(handle(command));
    }

    private sealed class HandlerWithoutValue(Result result) : ICommandHandler<string>
    {
        public Task<Result> HandleAsync(string command, CancellationToken cancellationToken) => Task.FromResult(result);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public List<string> Calls { get; } = [];

        public Result SaveResult { get; set; } = Result.Success();

        public Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
        {
            Calls.Add("save");
            return Task.FromResult(SaveResult);
        }

        public void DiscardChanges() => Calls.Add("discard");
    }
}
