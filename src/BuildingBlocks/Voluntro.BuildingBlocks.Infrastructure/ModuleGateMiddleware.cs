using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Voluntro.BuildingBlocks.Infrastructure;

/// <summary>
/// Answers 404 for every request to a disabled module, exactly as if the route didn't exist.
/// A controller belongs to the module whose assembly contains it; controllers outside any module are never gated.
/// </summary>
internal sealed class ModuleGateMiddleware(RequestDelegate next, IEnumerable<IModule> modules)
{
    // Built once: modules are registered at startup and never change. AddModule guarantees one module per assembly.
    private readonly Dictionary<Assembly, string> _moduleKeyByAssembly =
        modules.ToDictionary(module => module.GetType().Assembly, module => module.Key);

    // A module's routes all start with /api/{key} (API conventions).
    private readonly (PathString Prefix, string ModuleKey)[] _modulePrefixes =
        [.. modules.Select(module => (new PathString("/api/" + module.Key), module.Key))];

    // IModuleActivation is resolved per request, so Platform's implementation may have any lifetime.
    public Task InvokeAsync(HttpContext context, IModuleActivation activation)
    {
        var moduleKey = FindModuleKey(context);

        if (moduleKey is not null && !activation.IsEnabled(moduleKey))
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

    /// <summary>The key of the module this request belongs to, or null if it belongs to none.</summary>
    private string? FindModuleKey(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            // Nothing matched: already an unknown route.
            return null;
        }

        // A controller action: its assembly decides, whatever its route.
        var action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (action is not null)
        {
            return _moduleKeyByAssembly.GetValueOrDefault(action.ControllerTypeInfo.Assembly);
        }

        // Any other endpoint. Routing answers a known path with the wrong method or content type through an
        // endpoint of its own (405 with an Allow header, 415), which says nothing about the controller it stands
        // in for. Such a request to a disabled module's routes would reveal that they exist, so here the path decides.
        foreach (var (prefix, moduleKey) in _modulePrefixes)
        {
            if (context.Request.Path.StartsWithSegments(prefix))
            {
                return moduleKey;
            }
        }

        return null;
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
