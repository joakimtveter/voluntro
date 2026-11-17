using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure;

public static class HandlerServiceCollectionExtensions
{
    /// <summary>
    /// Starts registering a module's handlers, bound to <typeparamref name="TContext"/>: command handlers wrapped in
    /// the command pipeline (ADR-0012), and domain event handlers run by the module's unit of work (ADR-0013).
    /// Called from <see cref="IModule.Register"/>:
    /// <code>
    /// services.AddHandlers&lt;SampleDbContext&gt;()
    ///     .Add&lt;CreateSampleOrder, SampleOrderId, CreateSampleOrderHandler&gt;()
    ///     .AddDomainEventHandler&lt;SampleOrderPlaced, CountPlacedOrderHandler&gt;();
    /// </code>
    /// </summary>
    /// <typeparam name="TContext">The module's DbContext, registered with <c>AddModuleDbContext</c>.</typeparam>
    public static ModuleHandlerRegistrar<TContext> AddHandlers<TContext>(this IServiceCollection services)
        where TContext : ModuleDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        return new ModuleHandlerRegistrar<TContext>(services);
    }
}

/// <summary>
/// Registers a module's handlers, scoped, all bound to the module's own <typeparamref name="TContext"/> even when other
/// modules' contexts share the request scope. Command handlers are wrapped as Validation → Transaction → handler
/// (ADR-0012): <c>Add</c> leaves validation out, <c>AddValidated</c> puts the command's validator in front. Domain event
/// handlers are run by the module's unit of work just before it saves (ADR-0013).
/// </summary>
/// <remarks>
/// The handler and validator classes are registered only under a key that the registrar keeps to itself, so nothing
/// can inject a handler without its decorators. The container still builds them: it checks their constructors when it
/// validates (<c>ValidateOnBuild</c>, in Development) and disposes them with the scope. Every command pipeline is
/// recorded in <see cref="RegisteredHandlers"/>, so <c>AddModule</c> can refuse a handler registered any other way.
/// The overloads with a validator are named <c>AddValidated</c> because <c>Add&lt;TCommand, THandler, TValidator&gt;</c>
/// would clash with <c>Add&lt;TCommand, TResponse, THandler&gt;</c>.
/// The module's <see cref="IUnitOfWork"/> is registered keyed by the context type: tests resolve it with
/// <c>GetRequiredKeyedService&lt;IUnitOfWork&gt;(typeof(TContext))</c>, or register their own under that key before
/// calling <c>AddHandlers</c>.
/// </remarks>
public sealed class ModuleHandlerRegistrar<TContext>
    where TContext : ModuleDbContext
{
    // Private, so no one else can resolve the undecorated classes; [FromKeyedServices] needs the key itself.
    private static readonly object PipelineKey = new();

    private readonly IServiceCollection _services;
    private readonly RegisteredHandlers _registered;
    private readonly DomainEventHandlers<TContext> _domainEventHandlers;

    internal ModuleHandlerRegistrar(IServiceCollection services)
    {
        _services = services;
        _registered = RegisteredHandlers.In(services);
        _domainEventHandlers = DomainEventHandlers<TContext>.In(services);

        // Keyed by the context type, so each module saves through its own context. TryAdd: calling AddHandlers twice
        // for one context keeps one unit of work, and a test can register its own under the key first.
        // AddModuleDbContext adds the same clock; added here too, so the unit of work never lacks one.
        services.TryAddSingleton(TimeProvider.System);
        var domainEventHandlers = _domainEventHandlers;
        services.TryAddKeyedScoped<IUnitOfWork>(UnitOfWorkKey, (provider, _) =>
            new ModuleUnitOfWork<TContext>(
                provider.GetRequiredService<TContext>(),
                domainEventHandlers,
                provider.GetRequiredService<TimeProvider>(),
                provider));
    }

    // The key the module's IUnitOfWork is registered under: the context type. Tests resolve it with
    // GetRequiredKeyedService<IUnitOfWork>(typeof(TContext)), or register their own under it before calling AddHandlers.
    private static readonly Type UnitOfWorkKey = typeof(TContext);

    /// <summary>Registers a command handler that returns a value on success, without a validator.</summary>
    /// <exception cref="InvalidOperationException">
    /// A handler for <typeparamref name="TCommand"/> is already registered, or the command has a property that needs
    /// a validator.
    /// </exception>
    public ModuleHandlerRegistrar<TContext> Add<TCommand, TResponse, THandler>()
        where TResponse : notnull
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        EnsureNotRegistered<ICommandHandler<TCommand, TResponse>>();
        EnsureNothingToValidate<TCommand>();

        _services.TryAddKeyedScoped<THandler>(PipelineKey);
        AddPipeline<ICommandHandler<TCommand, TResponse>>(provider =>
            new TransactionDecorator<TCommand, TResponse>(
                provider.GetRequiredKeyedService<THandler>(PipelineKey),
                UnitOfWork(provider)));

        return this;
    }

    /// <summary>Registers a command handler that returns nothing on success, without a validator.</summary>
    /// <exception cref="InvalidOperationException">
    /// A handler for <typeparamref name="TCommand"/> is already registered, or the command has a property that needs
    /// a validator.
    /// </exception>
    public ModuleHandlerRegistrar<TContext> Add<TCommand, THandler>()
        where THandler : class, ICommandHandler<TCommand>
    {
        EnsureNotRegistered<ICommandHandler<TCommand>>();
        EnsureNothingToValidate<TCommand>();

        _services.TryAddKeyedScoped<THandler>(PipelineKey);
        AddPipeline<ICommandHandler<TCommand>>(provider =>
            new TransactionDecorator<TCommand>(
                provider.GetRequiredKeyedService<THandler>(PipelineKey),
                UnitOfWork(provider)));

        return this;
    }

    /// <summary>
    /// Registers a command handler that returns a value on success, validated by <typeparamref name="TValidator"/>
    /// before anything else runs.
    /// </summary>
    /// <exception cref="InvalidOperationException">A handler for <typeparamref name="TCommand"/> is already registered.</exception>
    public ModuleHandlerRegistrar<TContext> AddValidated<TCommand, TResponse, THandler, TValidator>()
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
                    UnitOfWork(provider)),
                provider.GetRequiredKeyedService<TValidator>(PipelineKey)));

        return this;
    }

    /// <summary>
    /// Registers a command handler that returns nothing on success, validated by <typeparamref name="TValidator"/>
    /// before anything else runs.
    /// </summary>
    /// <exception cref="InvalidOperationException">A handler for <typeparamref name="TCommand"/> is already registered.</exception>
    public ModuleHandlerRegistrar<TContext> AddValidated<TCommand, THandler, TValidator>()
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
                    UnitOfWork(provider)),
                provider.GetRequiredKeyedService<TValidator>(PipelineKey)));

        return this;
    }

    /// <summary>
    /// Registers a handler for a domain event raised by this module's aggregates. The module's unit of work runs it
    /// before the save, after any handlers registered earlier for the same event (ADR-0013).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="THandler"/> is already registered for <typeparamref name="TEvent"/>.
    /// </exception>
    public ModuleHandlerRegistrar<TContext> AddDomainEventHandler<TEvent, THandler>()
        where TEvent : IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>
    {
        _services.TryAddKeyedScoped<THandler>(PipelineKey);

        // TEvent is known here, so the cast is compiled in: the unit of work, which only sees IDomainEvent, calls it
        // without reflection.
        _domainEventHandlers.Add(typeof(TEvent), typeof(THandler), (provider, domainEvent, cancellationToken) =>
            provider.GetRequiredKeyedService<THandler>(PipelineKey).HandleAsync((TEvent)domainEvent, cancellationToken));

        return this;
    }

    private static IUnitOfWork UnitOfWork(IServiceProvider provider) =>
        provider.GetRequiredKeyedService<IUnitOfWork>(UnitOfWorkKey);

    // Recorded, so that AddModule can tell the pipeline from a handler registered directly, which would never save.
    private void AddPipeline<TService>(Func<IServiceProvider, TService> pipeline)
        where TService : class
    {
        var registration = ServiceDescriptor.Scoped(pipeline);
        _services.Add(registration);
        _registered.Add(registration);
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
/// The command pipelines registered by a <see cref="ModuleHandlerRegistrar{TContext}"/>. One instance is kept in the
/// service collection, so that <c>AddModule</c> can refuse a handler registered any other way.
/// </summary>
internal sealed class RegisteredHandlers
{
    private readonly HashSet<ServiceDescriptor> _registrations = [];

    /// <summary>The instance kept in <paramref name="services"/>, added on first use.</summary>
    public static RegisteredHandlers In(IServiceCollection services)
    {
        var registered = Find(services);
        if (registered is null)
        {
            registered = new RegisteredHandlers();
            services.AddSingleton(registered);
        }

        return registered;
    }

    public void Add(ServiceDescriptor registration) => _registrations.Add(registration);

    /// <summary>
    /// The commands and events with a handler in <paramref name="services"/> that no registrar put there. Command
    /// pipelines are compared by reference, so a direct registration is found even when the registrar handles the same
    /// command. The registrar never registers an <see cref="IDomainEventHandler{TEvent}"/> as such (only the handler
    /// class, under its private key), so every one in the container was registered directly.
    /// </summary>
    public static IReadOnlyList<Type> HandledDirectly(IServiceCollection services)
    {
        var registered = Find(services);

        return services
            .Where(registration => IsHandler(registration.ServiceType)
                && registered?._registrations.Contains(registration) != true)
            .Select(registration => registration.ServiceType.GetGenericArguments()[0])
            .Distinct()
            .ToList();
    }

    // Filtered by service type first: ImplementationInstance throws on a keyed registration.
    private static RegisteredHandlers? Find(IServiceCollection services) =>
        services
            .Where(registration => registration.ServiceType == typeof(RegisteredHandlers))
            .Select(registration => registration.ImplementationInstance)
            .OfType<RegisteredHandlers>()
            .SingleOrDefault();

    private static bool IsHandler(Type serviceType)
    {
        if (!serviceType.IsGenericType)
        {
            return false;
        }

        var definition = serviceType.GetGenericTypeDefinition();
        return definition == typeof(ICommandHandler<>)
            || definition == typeof(ICommandHandler<,>)
            || definition == typeof(IDomainEventHandler<>);
    }
}
