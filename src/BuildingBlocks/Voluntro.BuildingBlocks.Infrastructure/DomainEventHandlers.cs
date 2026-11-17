using Microsoft.Extensions.DependencyInjection;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure;

/// <summary>
/// Calls one registered <see cref="IDomainEventHandler{TEvent}"/>, resolved from <paramref name="services"/>, with an
/// event the caller knows only as <see cref="IDomainEvent"/>.
/// </summary>
internal delegate Task<Result> HandleDomainEvent(
    IServiceProvider services,
    IDomainEvent domainEvent,
    CancellationToken cancellationToken);

/// <summary>
/// The domain event handlers registered for one module's context, in registration order. One instance per context is
/// kept in the service collection, so every <c>AddHandlers&lt;TContext&gt;()</c> call for that context adds to it, and
/// the module's unit of work reads it.
/// </summary>
internal sealed class DomainEventHandlers<TContext>
    where TContext : ModuleDbContext
{
    private readonly List<Registration> _registrations = [];

    /// <summary>The instance kept in <paramref name="services"/>, added on first use.</summary>
    public static DomainEventHandlers<TContext> In(IServiceCollection services)
    {
        // Filtered by service type first: ImplementationInstance throws on a keyed registration.
        var handlers = services
            .Where(registration => registration.ServiceType == typeof(DomainEventHandlers<TContext>))
            .Select(registration => registration.ImplementationInstance)
            .OfType<DomainEventHandlers<TContext>>()
            .SingleOrDefault();

        if (handlers is null)
        {
            handlers = new DomainEventHandlers<TContext>();
            services.AddSingleton(handlers);
        }

        return handlers;
    }

    /// <exception cref="InvalidOperationException">The handler is already registered for the event.</exception>
    public void Add(Type eventType, Type handlerType, HandleDomainEvent handle)
    {
        // Registered twice, it would run twice for every event.
        if (_registrations.Any(registration => registration.EventType == eventType && registration.HandlerType == handlerType))
        {
            throw new InvalidOperationException(
                $"{handlerType.Name} is already registered as a handler for {eventType.Name} in {typeof(TContext).Name}.");
        }

        _registrations.Add(new Registration(eventType, handlerType, handle));
    }

    /// <summary>
    /// The handlers for an event of exactly <paramref name="eventType"/>, in registration order. The registrar accepts
    /// only sealed event types, so there is no handler for a base type or interface to miss.
    /// </summary>
    public IEnumerable<HandleDomainEvent> For(Type eventType) =>
        _registrations
            .Where(registration => registration.EventType == eventType)
            .Select(registration => registration.Handle);

    private sealed record Registration(Type EventType, Type HandlerType, HandleDomainEvent Handle);
}
