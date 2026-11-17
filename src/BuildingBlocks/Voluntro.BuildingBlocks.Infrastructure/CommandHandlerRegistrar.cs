using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;
using Voluntro.SharedKernel;

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
/// validates (<c>ValidateOnBuild</c>, in Development) and disposes them with the scope. Every registration is recorded
/// in <see cref="PipelinedCommandHandlers"/>, so <c>AddModule</c> can refuse a command handler registered any other
/// way. The overloads with a validator are named <c>AddValidated</c> because
/// <c>Add&lt;TCommand, THandler, TValidator&gt;</c> would clash with <c>Add&lt;TCommand, TResponse, THandler&gt;</c>.
/// </remarks>
public sealed class CommandHandlerRegistrar<TContext>
    where TContext : ModuleDbContext
{
    // Private, so no one else can resolve the undecorated classes; [FromKeyedServices] needs the key itself.
    private static readonly object PipelineKey = new();

    private readonly IServiceCollection _services;
    private readonly PipelinedCommandHandlers _pipelined;

    internal CommandHandlerRegistrar(IServiceCollection services)
    {
        _services = services;
        _pipelined = PipelinedCommandHandlers.In(services);
    }

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
        AddPipeline<ICommandHandler<TCommand, TResponse>>(provider =>
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
        AddPipeline<ICommandHandler<TCommand>>(provider =>
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
        AddPipeline<ICommandHandler<TCommand, TResponse>>(provider =>
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
        AddPipeline<ICommandHandler<TCommand>>(provider =>
            new ValidationDecorator<TCommand>(
                new TransactionDecorator<TCommand>(
                    provider.GetRequiredKeyedService<THandler>(PipelineKey),
                    provider.GetRequiredService<TContext>()),
                provider.GetRequiredKeyedService<TValidator>(PipelineKey)));

        return this;
    }

    // Recorded, so that AddModule can tell the pipeline from a handler registered directly, which would never save.
    private void AddPipeline<TService>(Func<IServiceProvider, TService> pipeline)
        where TService : class
    {
        var registration = ServiceDescriptor.Scoped(pipeline);
        _services.Add(registration);
        _pipelined.Add(registration);
    }

    // A second registration would silently win over the first.
    private void EnsureNotRegistered<TService>()
    {
        if (_services.Any(descriptor => descriptor.ServiceType == typeof(TService)))
        {
            throw new InvalidOperationException($"A handler for {typeof(TService)} is already registered.");
        }
    }

    // Two kinds of property need a validator, because a client can leave them out:
    // - A non-nullable reference property. MVC binds a missing property as null: AddApiProblemDetails turns off its
    //   implicit [Required], so that validators can answer `required`. Without one the handler gets null: a 500.
    // - A Version. A missing one binds as 0 and fails EnsureVersion: a 409 that no reload can fix, where the
    //   validator's RequireVersion answers 400.
    private static void EnsureNothingToValidate<TCommand>()
    {
        var properties = typeof(TCommand).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        var nullability = new NullabilityInfoContext();
        var notNullable = properties
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

        if (properties.Any(property => property.Name == nameof(AggregateRoot<>.Version) && property.PropertyType == typeof(uint)))
        {
            throw new InvalidOperationException(
                $"{typeof(TCommand).Name} has a Version, which a client can leave out: a missing one binds as 0 and comes " +
                "back as a concurrency conflict. Register it with AddValidated and call errors.RequireVersion(instance.Version).");
        }
    }
}

/// <summary>
/// The command handler registrations made by a <see cref="CommandHandlerRegistrar{TContext}"/>. One instance is kept
/// in the service collection, so that <c>AddModule</c> can refuse a command handler registered any other way.
/// </summary>
internal sealed class PipelinedCommandHandlers
{
    private readonly HashSet<ServiceDescriptor> _registrations = [];

    /// <summary>The instance kept in <paramref name="services"/>, added on first use.</summary>
    public static PipelinedCommandHandlers In(IServiceCollection services)
    {
        var pipelined = Find(services);
        if (pipelined is null)
        {
            pipelined = new PipelinedCommandHandlers();
            services.AddSingleton(pipelined);
        }

        return pipelined;
    }

    public void Add(ServiceDescriptor registration) => _registrations.Add(registration);

    /// <summary>
    /// The commands with a handler in <paramref name="services"/> that no registrar put there. Registrations are
    /// compared by reference, so a direct registration is found even when the registrar handles the same command.
    /// </summary>
    public static IReadOnlyList<Type> CommandsRegisteredDirectly(IServiceCollection services)
    {
        var pipelined = Find(services);

        return services
            .Where(registration => IsCommandHandler(registration.ServiceType)
                && pipelined?._registrations.Contains(registration) != true)
            .Select(registration => registration.ServiceType.GetGenericArguments()[0])
            .Distinct()
            .ToList();
    }

    // Filtered by service type first: ImplementationInstance throws on a keyed registration.
    private static PipelinedCommandHandlers? Find(IServiceCollection services) =>
        services
            .Where(registration => registration.ServiceType == typeof(PipelinedCommandHandlers))
            .Select(registration => registration.ImplementationInstance)
            .OfType<PipelinedCommandHandlers>()
            .SingleOrDefault();

    private static bool IsCommandHandler(Type serviceType)
    {
        if (!serviceType.IsGenericType)
        {
            return false;
        }

        var definition = serviceType.GetGenericTypeDefinition();
        return definition == typeof(ICommandHandler<>) || definition == typeof(ICommandHandler<,>);
    }
}
