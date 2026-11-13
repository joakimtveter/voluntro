using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Voluntro.BuildingBlocks.Infrastructure;

/// <summary>The API's JSON rules: camelCase names, enums as camelCase strings, null properties omitted.</summary>
public static class JsonConventions
{
    public static void Apply(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Already the ASP.NET Core web default; stated so the whole convention is in one place.
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        // Strings both ways: "published", never 1. Rejecting integers on input keeps the contract unambiguous.
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
    }

    /// <summary>
    /// Applies <see cref="Apply"/> to both of ASP.NET Core's JSON settings: MVC's (controllers) and the HTTP ones
    /// (ProblemDetails outside MVC, and the OpenAPI document generator), so responses and the schema agree.
    /// </summary>
    public static IServiceCollection AddApiJsonConventions(this IServiceCollection services)
    {
        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options => Apply(options.JsonSerializerOptions));
        services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => Apply(options.SerializerOptions));
        return services;
    }
}
