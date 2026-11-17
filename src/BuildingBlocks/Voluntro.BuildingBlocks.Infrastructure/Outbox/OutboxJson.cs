using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Voluntro.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// How integration events are written to and read from the outbox: camelCase, enums as camelCase strings.
/// Fixed here and deliberately not the API's <c>JsonConventions</c>: a stored payload must read the same after the
/// API's conventions change.
/// </summary>
internal static class OutboxJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Serialize<TEvent>(TEvent integrationEvent) =>
        JsonSerializer.Serialize(integrationEvent, Options);

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));

        // Shared by every module and thread: no one may change it after the first use.
        options.MakeReadOnly();
        return options;
    }
}
