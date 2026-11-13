using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// The command pipeline runs Validation → Transaction → handler (ADR-0012), proven by what happens rather than by
/// the decorators' types: the validator, the handler and the unit of work record their calls in one log. With the
/// transaction outermost, an invalid command would also be discarded; with the handler outermost, it would run.
/// </summary>
/// <remarks>
/// The handlers are registered through the real registrar. The unit of work is a module context that re-implements
/// <see cref="IUnitOfWork"/>, so the registrar resolves it exactly as it resolves a real module's context.
/// </remarks>
public sealed class CommandPipelineOrderTests
{
    public static TheoryData<Step, string[]> Scenarios => new()
    {
        { Step.None, ["validate", "handle", "save"] },
        { Step.Validation, ["validate"] },
        { Step.Handler, ["validate", "handle", "discard"] },
    };

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task A_command_with_a_value_runs_validation_then_the_handler_then_the_save(Step fails, string[] expected)
    {
        using var provider = Build();
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<WithValue, int>>();

        await handler.HandleAsync(new WithValue(fails), TestContext.Current.CancellationToken);

        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<CallLog>().Calls);
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task A_command_without_a_value_runs_validation_then_the_handler_then_the_save(Step fails, string[] expected)
    {
        using var provider = Build();
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<WithoutValue>>();

        await handler.HandleAsync(new WithoutValue(fails), TestContext.Current.CancellationToken);

        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<CallLog>().Calls);
    }

    private static ServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddScoped<CallLog>();
        services.AddDbContext<RecordingDbContext>(options => options.UseNpgsql("Host=never-connected"));

        services.AddCommandHandlers<RecordingDbContext>()
            .AddValidated<WithValue, int, WithValueHandler, WithValueValidator>()
            .AddValidated<WithoutValue, WithoutValueHandler, WithoutValueValidator>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    /// <summary>Which step of the pipeline fails, if any.</summary>
    public enum Step
    {
        None,
        Validation,
        Handler,
    }

    private static readonly FieldError Invalid = new("name", FieldErrorCodes.Required);

    private static readonly Error Refused = Error.Conflict("test.command.refused");

    private sealed record WithValue(Step Fails);

    private sealed record WithoutValue(Step Fails);

    private sealed class CallLog
    {
        public List<string> Calls { get; } = [];
    }

    private sealed class WithValueValidator(CallLog log) : IValidator<WithValue>
    {
        public IReadOnlyList<FieldError> Validate(WithValue instance)
        {
            log.Calls.Add("validate");
            return instance.Fails == Step.Validation ? [Invalid] : [];
        }
    }

    private sealed class WithoutValueValidator(CallLog log) : IValidator<WithoutValue>
    {
        public IReadOnlyList<FieldError> Validate(WithoutValue instance)
        {
            log.Calls.Add("validate");
            return instance.Fails == Step.Validation ? [Invalid] : [];
        }
    }

    private sealed class WithValueHandler(CallLog log) : ICommandHandler<WithValue, int>
    {
        public Task<Result<int>> HandleAsync(WithValue command, CancellationToken cancellationToken)
        {
            log.Calls.Add("handle");
            return Task.FromResult<Result<int>>(command.Fails == Step.Handler ? Refused : 42);
        }
    }

    private sealed class WithoutValueHandler(CallLog log) : ICommandHandler<WithoutValue>
    {
        public Task<Result> HandleAsync(WithoutValue command, CancellationToken cancellationToken)
        {
            log.Calls.Add("handle");
            return Task.FromResult(command.Fails == Step.Handler ? Result.Failure(Refused) : Result.Success());
        }
    }

    /// <summary>
    /// A module context whose unit of work only records. Re-implementing the interface replaces
    /// <see cref="ModuleDbContext"/>'s own explicit implementation, so nothing reaches a database.
    /// </summary>
    private sealed class RecordingDbContext(DbContextOptions<RecordingDbContext> options, CallLog log)
        : ModuleDbContext(options), IUnitOfWork
    {
        protected override string Schema => "recording";

        Task<Result> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
        {
            log.Calls.Add("save");
            return Task.FromResult(Result.Success());
        }

        void IUnitOfWork.DiscardChanges() => log.Calls.Add("discard");

        protected override void ConfigureModel(ModelBuilder modelBuilder)
        {
        }
    }
}
