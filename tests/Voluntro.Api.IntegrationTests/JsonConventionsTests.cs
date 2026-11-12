using System.Text.Json;
using Voluntro.Api.IntegrationTests.Sample;

namespace Voluntro.Api.IntegrationTests;

/// <summary>JSON responses use camelCase names, enums as camelCase strings, and omit null properties.</summary>
public sealed class JsonConventionsTests(VoluntroApiFactory factory) : IClassFixture<VoluntroApiFactory>
{
    [Fact]
    public async Task Property_names_are_camelCase()
    {
        using var body = await GetItemAsync(SampleItems.First.Id);

        Assert.Equal(
            ["id", "name", "note", "status"],
            body.RootElement.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Enums_are_camelCase_strings()
    {
        using var body = await GetItemAsync(SampleItems.First.Id);

        var status = body.RootElement.GetProperty("status");
        Assert.Equal(JsonValueKind.String, status.ValueKind);
        Assert.Equal("published", status.GetString());
    }

    [Fact]
    public async Task Null_properties_are_omitted()
    {
        using var body = await GetItemAsync(SampleItems.Second.Id);

        Assert.False(body.RootElement.TryGetProperty("note", out _), "A null Note must not appear in the JSON.");
    }

    private async Task<JsonDocument> GetItemAsync(Guid id)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/sample/items/{id}", ct);

        response.EnsureSuccessStatusCode();
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }
}
