using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Voluntro.BuildingBlocks.Infrastructure;

/// <summary>
/// Describes query parameters in camelCase (<c>?pageSize=25</c>), like JSON properties.
/// </summary>
/// <remarks>
/// MVC describes a query parameter by its C# name, so <c>[FromQuery] PageRequest</c> is documented as
/// <c>Page</c>, <c>PageSize</c>, … Binding ignores case, so requests work either way, but the OpenAPI document
/// (and the frontend types generated from it) would show names the API conventions don't use.
/// This renames them in the API description, which the OpenAPI document is built from.
/// A name set explicitly, e.g. <c>[FromQuery(Name = "q")]</c>, is left alone.
/// </remarks>
internal sealed class CamelCaseQueryParameterNames : IApiDescriptionProvider
{
    // Providers run in ascending order; MVC's own provider (-1000) creates the descriptions this one renames.
    public int Order => 0;

    public void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
        foreach (var parameter in context.Results.SelectMany(description => description.ParameterDescriptions))
        {
            if (parameter.Source == BindingSource.Query && parameter.ModelMetadata?.BinderModelName is null)
            {
                parameter.Name = JsonNamingPolicy.CamelCase.ConvertName(parameter.Name);
            }
        }
    }

    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
    }
}

/// <summary>
/// Binds an enum in a route or query value from its name only (any casing), never from the integer behind it,
/// matching what <see cref="JsonConventions"/> does for JSON bodies.
/// </summary>
/// <remarks>
/// MVC's own enum binder also accepts the integer of a defined value: <c>/errors/3</c> binds like
/// <c>/errors/conflict</c>. This binder turns anything that is not one of the enum's names into the same
/// "value is not valid" error MVC gives for an undefined value, and leaves the rest to MVC's binder.
/// </remarks>
internal sealed class EnumNameModelBinder(Type enumType, IModelBinder builtIn) : IModelBinder
{
    private readonly string[] _names = Enum.GetNames(enumType);

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var result = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        var value = result.FirstValue;

        // A missing or empty value is not this binder's business: MVC decides whether that is allowed.
        if (string.IsNullOrWhiteSpace(value) || _names.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return builtIn.BindModelAsync(bindingContext);
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, result);
        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            bindingContext.ModelMetadata.ModelBindingMessageProvider.ValueIsInvalidAccessor(value));
        return Task.CompletedTask;
    }
}

internal sealed class EnumNameModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // IsEnum is also true for a nullable enum; UnderlyingOrModelType is then the enum itself.
        if (!context.Metadata.IsEnum)
        {
            return null;
        }

        var enumType = context.Metadata.UnderlyingOrModelType;

        // MVC's binder. Its first argument has no effect any more (see its documentation).
        var builtIn = new EnumTypeModelBinder(
            suppressBindingUndefinedValueToEnumType: true,
            enumType,
            context.Services.GetRequiredService<ILoggerFactory>());

        return new EnumNameModelBinder(enumType, builtIn);
    }
}

public static class ParameterConventions
{
    /// <summary>
    /// The rules for values bound from the route and the query string: query parameters are documented in
    /// camelCase (so the OpenAPI document matches the API conventions), and enums bind from their names only.
    /// </summary>
    public static IServiceCollection AddApiParameterConventions(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, CamelCaseQueryParameterNames>());

        // First in the list, so it is asked before MVC's own enum binder provider.
        services.Configure<MvcOptions>(options => options.ModelBinderProviders.Insert(0, new EnumNameModelBinderProvider()));

        return services;
    }
}
