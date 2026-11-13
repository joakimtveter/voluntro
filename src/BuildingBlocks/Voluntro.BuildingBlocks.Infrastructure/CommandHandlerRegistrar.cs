using Microsoft.Extensions.DependencyInjection;
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
/// Registers a module's command handlers, scoped, each wrapped as Transaction → handler, saving through the module's
/// own <typeparamref name="TContext"/> even when other modules' contexts share the request scope (ADR-0012).
/// </summary>
/// <remarks>
/// The handler class itself is not registered, only the decorated interface, so nothing can inject a handler
/// without its decorators.
/// </remarks>
public sealed class CommandHandlerRegistrar<TContext>
    where TContext : ModuleDbContext
{
    private readonly IServiceCollection _services;

    internal CommandHandlerRegistrar(IServiceCollection services) => _services = services;

    /// <summary>Registers a command handler that returns a value on success.</summary>
    /// <exception cref="InvalidOperationException">A handler for <typeparamref name="TCommand"/> is already registered.</exception>
    public CommandHandlerRegistrar<TContext> Add<TCommand, TResponse, THandler>()
        where TResponse : notnull
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        EnsureNotRegistered<ICommandHandler<TCommand, TResponse>>();

        _services.AddScoped<ICommandHandler<TCommand, TResponse>>(provider =>
            new TransactionDecorator<TCommand, TResponse>(
                ActivatorUtilities.CreateInstance<THandler>(provider),
                provider.GetRequiredService<TContext>()));

        return this;
    }

    /// <summary>Registers a command handler that returns nothing on success.</summary>
    /// <exception cref="InvalidOperationException">A handler for <typeparamref name="TCommand"/> is already registered.</exception>
    public CommandHandlerRegistrar<TContext> Add<TCommand, THandler>()
        where THandler : class, ICommandHandler<TCommand>
    {
        EnsureNotRegistered<ICommandHandler<TCommand>>();

        _services.AddScoped<ICommandHandler<TCommand>>(provider =>
            new TransactionDecorator<TCommand>(
                ActivatorUtilities.CreateInstance<THandler>(provider),
                provider.GetRequiredService<TContext>()));

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
}
