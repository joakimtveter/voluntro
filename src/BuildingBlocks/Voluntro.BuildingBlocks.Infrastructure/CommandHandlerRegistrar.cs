using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;

namespace Voluntro.BuildingBlocks.Infrastructure;

public static class CommandHandlerServiceCollectionExtensions
{
    /// <summary>
    /// Starts registering a module's command handlers, each wrapped in the command pipeline and saved through
    /// <typeparamref name="TContext"/> (ADR-0012). Called from <see cref="IModule.Register"/>:
    /// <code>
    /// services.AddCommandHandlers&lt;SampleDbContext&gt;()
    ///     .Add&lt;CreateSampleOrder, SampleOrderId, CreateSampleOrderHandler&gt;();
    /// </code>
    /// </summary>
    /// <typeparam name="TContext">The module's DbContext, registered with <c>AddModuleDbContext</c>.</typeparam>
    public static CommandHandlerRegistrar<TContext> AddCommandHandlers<TContext>(this IServiceCollection services)
        where TContext : ModuleDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        return new CommandHandlerRegistrar<TContext>(services);
    }
}

/// <summary>
/// Registers a module's command handlers, scoped, each wrapped as Validation → Transaction → handler, saving through
/// the module's own <typeparamref name="TContext"/> even when other modules' contexts share the request scope
/// (ADR-0012). <c>Add</c> leaves validation out; <c>AddValidated</c> puts the command's validator in front.
/// </summary>
/// <remarks>
/// The handler and validator classes are registered only under a key that the registrar keeps to itself, so nothing
/// can inject a handler without its decorators. The container still builds them: it checks their constructors when it
/// validates (<c>ValidateOnBuild</c>, in Development) and disposes them with the scope. The overloads with a validator
/// are named <c>AddValidated</c> because <c>Add&lt;TCommand, THandler, TValidator&gt;</c> would clash with
/// <c>Add&lt;TCommand, TResponse, THandler&gt;</c>.
/// </remarks>
public sealed class CommandHandlerRegistrar<TContext>
    where TContext : ModuleDbContext
{
    // Private, so no one else can resolve the undecorated classes; [FromKeyedServices] needs the key itself.
    private static readonly object PipelineKey = new();

    private readonly IServiceCollection _services;

    internal CommandHandlerRegistrar(IServiceCollection services) => _services = services;

    /// <summary>Registers a command handler that returns a value on success, without a validator.</summary>
    /// <exception cref="InvalidOperationException">
    /// A handler for <typeparamref name="TCommand"/> is already registered, or the command has a property that needs
    /// a validator.
    /// </exception>
    public CommandHandlerRegistrar<TContext> Add<TCommand, TResponse, THandler>()
        where TResponse : notnull
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        EnsureNotRegistered<ICommandHandler<TCommand, TResponse>>();
        EnsureNothingToValidate<TCommand>();

        _services.TryAddKeyedScoped<THandler>(PipelineKey);
        _services.AddScoped<ICommandHandler<TCommand, TResponse>>(provider =>
            new TransactionDecorator<TCommand, TResponse>(
                provider.GetRequiredKeyedService<THandler>(PipelineKey),
                provider.GetRequiredService<TContext>()));

        return this;
    }

    /// <summary>Registers a command handler that returns nothing on success, without a validator.</summary>
    /// <exception cref="InvalidOperationException">
    /// A handler for <typeparamref name="TCommand"/> is already registered, or the command has a property that needs
    /// a validator.
    /// </exception>
    public CommandHandlerRegistrar<TContext> Add<TCommand, THandler>()
        where THandler : class, ICommandHandler<TCommand>
    {
        EnsureNotRegistered<ICommandHandler<TCommand>>();
        EnsureNothingToValidate<TCommand>();

        _services.TryAddKeyedScoped<THandler>(PipelineKey);
        _services.AddScoped<ICommandHandler<TCommand>>(provider =>
            new TransactionDecorator<TCommand>(
                provider.GetRequiredKeyedService<THandler>(PipelineKey),
                provider.GetRequiredService<TContext>()));

        return this;
    }

    /// <summary>
    /// Registers a command handler that returns a value on success, validated by <typeparamref name="TValidator"/>
    /// before anything else runs.
    /// </summary>
    /// <exception cref="InvalidOperationException">A handler for <typeparamref name="TCommand"/> is already registered.</exception>
    public CommandHandlerRegistrar<TContext> AddValidated<TCommand, TResponse, THandler, TValidator>()
        where TResponse : notnull
        where THandler : class, ICommandHandler<TCommand, TResponse>
        where TValidator : class, IValidator<TCommand>
    {
        EnsureNotRegistered<ICommandHandler<TCommand, TResponse>>();

        _services.TryAddKeyedScoped<THandler>(PipelineKey);
        _services.TryAddKeyedScoped<TValidator>(PipelineKey);
        _services.AddScoped<ICommandHandler<TCommand, TResponse>>(provider =>
            new ValidationDecorator<TCommand, TResponse>(
                new TransactionDecorator<TCommand, TResponse>(
                    provider.GetRequiredKeyedService<THandler>(PipelineKey),
                    provider.GetRequiredService<TContext>()),
                provider.GetRequiredKeyedService<TValidator>(PipelineKey)));

        return this;
    }

    /// <summary>
    /// Registers a command handler that returns nothing on success, validated by <typeparamref name="TValidator"/>
    /// before anything else runs.
    /// </summary>
    /// <exception cref="InvalidOperationException">A handler for <typeparamref name="TCommand"/> is already registered.</exception>
    public CommandHandlerRegistrar<TContext> AddValidated<TCommand, THandler, TValidator>()
        where THandler : class, ICommandHandler<TCommand>
        where TValidator : class, IValidator<TCommand>
    {
        EnsureNotRegistered<ICommandHandler<TCommand>>();

        _services.TryAddKeyedScoped<THandler>(PipelineKey);
        _services.TryAddKeyedScoped<TValidator>(PipelineKey);
        _services.AddScoped<ICommandHandler<TCommand>>(provider =>
            new ValidationDecorator<TCommand>(
                new TransactionDecorator<TCommand>(
                    provider.GetRequiredKeyedService<THandler>(PipelineKey),
                    provider.GetRequiredService<TContext>()),
                provider.GetRequiredKeyedService<TValidator>(PipelineKey)));

        return this;
    }

    // A second registration would silently win over the first.
    private void EnsureNotRegistered<TService>()
    {
        if (_services.Any(descriptor => descriptor.ServiceType == typeof(TService)))
        {
            throw new InvalidOperationException($"A handler for {typeof(TService)} is already registered.");
        }
    }

    // MVC binds a missing property as null: AddApiProblemDetails turns off its implicit [Required], so that validators
    // can answer `required`. A command with a non-nullable reference property therefore needs a validator, or a
    // client that leaves the property out reaches the handler with null and gets a 500.
    private static void EnsureNothingToValidate<TCommand>()
    {
        var nullability = new NullabilityInfoContext();
        var notNullable = typeof(TCommand)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => !property.PropertyType.IsValueType
                && nullability.Create(property).ReadState == NullabilityState.NotNull)
            .Select(property => property.Name)
            .ToList();

        if (notNullable.Count > 0)
        {
            throw new InvalidOperationException(
                $"{typeof(TCommand).Name} has non-nullable properties a client can leave out ({string.Join(", ", notNullable)}): " +
                "a missing one binds as null. Register it with AddValidated and check them for null, or make them nullable.");
        }
    }
}
