using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Voluntro.BuildingBlocks.Infrastructure;

public static class ModuleServiceCollectionExtensions
{
    /// <summary>Registers each module in <paramref name="modules"/>. Called once from <c>Program.cs</c>.</summary>
    public static IHostApplicationBuilder AddModules(this IHostApplicationBuilder builder, params IModule[] modules)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(modules);

        // Until Platform registers the real activation source, every module is enabled.
        builder.Services.TryAddSingleton<IModuleActivation, AllModulesEnabled>();

        foreach (var module in modules)
        {
            builder.Services.AddModule(module, builder.Configuration);
        }

        return builder;
    }

    /// <summary>
    /// The one way a module is added, used by <c>Program.cs</c> and by the integration-test harness alike.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// A module with the same key, or another module in the same assembly, is already registered; or a command or
    /// domain event handler is registered without <c>AddHandlers</c>.
    /// </exception>
    public static IServiceCollection AddModule(this IServiceCollection services, IModule module, IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(config);

        EnsureNoClash(services, module);

        // 1. The module registers its own services (handlers, later its DbContext, consumers, ...).
        module.Register(services, config);
        EnsureHandlersCameFromTheRegistrar(services, module);

        // 2. Make the module itself available, e.g. to the module gate and GET /api/platform/modules.
        //    Registered as IModule (not as its concrete type) so IEnumerable<IModule> lists them all.
        services.AddSingleton<IModule>(module);

        // 3. MVC only discovers controllers in assemblies it knows about ("application parts").
        //    The module's controllers live in the same assembly as its IModule.
        services.AddControllers().AddApplicationPart(module.GetType().Assembly);

        return services;
    }

    // Handlers never save (ADR-0012): a command handler registered without the registrar's pipeline would answer
    // success and persist nothing. A domain event handler registered directly would never be called: the unit of work
    // only runs the ones the registrar recorded (ADR-0013).
    private static void EnsureHandlersCameFromTheRegistrar(IServiceCollection services, IModule module)
    {
        var direct = RegisteredHandlers.HandledDirectly(services);
        if (direct.Count > 0)
        {
            throw new InvalidOperationException(
                $"After registering module '{module.Key}', the handlers for these commands or events are registered " +
                $"directly in the container: {string.Join(", ", direct.Select(message => message.Name))}. A command " +
                "handler would run without the command pipeline, so nothing would save its changes, and a domain event " +
                "handler would never be called. Register them with AddHandlers<TContext>() instead.");
        }
    }

    private static void EnsureNoClash(IServiceCollection services, IModule module)
    {
        var registered = services
            .Where(descriptor => descriptor.ServiceType == typeof(IModule))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<IModule>()
            .ToList();

        if (registered.Any(other => string.Equals(other.Key, module.Key, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"A module with key '{module.Key}' is already registered.");
        }

        var assembly = module.GetType().Assembly;
        var sameAssembly = registered.FirstOrDefault(other => other.GetType().Assembly == assembly);
        if (sameAssembly is not null)
        {
            throw new InvalidOperationException(
                $"Module '{module.Key}' is in assembly {assembly.GetName().Name}, which already holds module " +
                $"'{sameAssembly.Key}'. Controllers belong to the module in their assembly, so each module needs its own.");
        }
    }
}
