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
/// Registers a module's command handlers, scoped, each wrapped as Validation → Transaction → handler, saving through
/// the module's own <typeparamref name="TContext"/> even when other modules' contexts share the request scope
/// (ADR-0012). <c>Add</c> leaves validation out; <c>AddValidated</c> puts the command's validator in front.
/// </summary>
/// <remarks>
/// Neither the handler class nor the validator is registered, only the decorated interface, so nothing can inject
/// a handler without its decorators. The overloads with a validator are named <c>AddValidated</c> because
/// <c>Add&lt;TCommand, THandler, TValidator&gt;</c> would clash with <c>Add&lt;TCommand, TResponse, THandler&gt;</c>.
/// </remarks>
public sealed class CommandHandlerRegistrar<TContext>
    where TContext : ModuleDbContext
{
    private readonly IServiceCollection _services;

    internal CommandHandlerRegistrar(IServiceCollection services) => _services = services;

    /// <summary>Registers a command handler that returns a value on success, without a validator.</summary>
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

    /// <summary>Registers a command handler that returns nothing on success, without a validator.</summary>
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

        _services.AddScoped<ICommandHandler<TCommand, TResponse>>(provider =>
            new ValidationDecorator<TCommand, TResponse>(
                new TransactionDecorator<TCommand, TResponse>(
                    ActivatorUtilities.CreateInstance<THandler>(provider),
                    provider.GetRequiredService<TContext>()),
                ActivatorUtilities.CreateInstance<TValidator>(provider)));

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

        _services.AddScoped<ICommandHandler<TCommand>>(provider =>
            new ValidationDecorator<TCommand>(
                new TransactionDecorator<TCommand>(
                    ActivatorUtilities.CreateInstance<THandler>(provider),
                    provider.GetRequiredService<TContext>()),
                ActivatorUtilities.CreateInstance<TValidator>(provider)));

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
