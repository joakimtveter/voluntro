using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

public static class QueryParameterConventions
{
    /// <summary>Documents query parameters in camelCase, so the OpenAPI document matches the API conventions.</summary>
    public static IServiceCollection AddApiQueryParameterConventions(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, CamelCaseQueryParameterNames>());
        return services;
    }
}
