using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Voluntro.BuildingBlocks.Infrastructure;

/// <summary>
/// Answers 404 for every endpoint of a disabled module, exactly as if the route didn't exist.
/// A controller belongs to the module whose assembly contains it; controllers outside any module are never gated.
/// </summary>
internal sealed class ModuleGateMiddleware(RequestDelegate next, IEnumerable<IModule> modules)
{
    // Built once: modules are registered at startup and never change. AddModule guarantees one module per assembly.
    private readonly Dictionary<Assembly, string> _moduleKeyByAssembly =
        modules.ToDictionary(module => module.GetType().Assembly, module => module.Key);

    // IModuleActivation is resolved per request, so Platform's implementation may have any lifetime.
    public Task InvokeAsync(HttpContext context, IModuleActivation activation)
    {
        var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();

        if (action is not null
            && _moduleKeyByAssembly.TryGetValue(action.ControllerTypeInfo.Assembly, out var moduleKey)
            && !activation.IsEnabled(moduleKey))
        {
            // Behave exactly like an unknown route: no endpoint, 404, no body; status-code pages adds the body.
            // Clearing the endpoint is a safeguard, not a current fix: without it MVC's ProblemDetails writer handles
            // this response and today happens to produce the same body. With it, the request takes the unrouted path
            // itself, so endpoint metadata (e.g. [SkipStatusCodePages]) can never make the two differ.
            context.SetEndpoint(null);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }

        return next(context);
    }
}

public static class ModuleGateApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the module gate. Place it after routing and before authentication and authorization,
    /// so a disabled module answers 404 before a 401/403 could reveal that it exists.
    /// </summary>
    public static IApplicationBuilder UseModuleGate(this IApplicationBuilder app) =>
        app.UseMiddleware<ModuleGateMiddleware>();
}
