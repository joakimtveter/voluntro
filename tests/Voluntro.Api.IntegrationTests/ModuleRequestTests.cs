using System.Net;
using System.Text.Json;
using Voluntro.Api.IntegrationTests.Sample;

namespace Voluntro.Api.IntegrationTests;

public sealed class ModuleRequestTests(VoluntroApiFactory factory) : IClassFixture<VoluntroApiFactory>
{
    [Fact]
    public async Task An_existing_item_is_returned_through_its_query_handler()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/sample/items/{SampleItems.First.Id}", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        Assert.Equal(SampleItems.First.Id, body.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(SampleItems.First.Name, body.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task An_unknown_route_returns_404()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/does-not-exist", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
