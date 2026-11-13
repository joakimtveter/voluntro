using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Voluntro.Api.IntegrationTests.Database;

namespace Voluntro.Api.IntegrationTests;

/// <summary>An aggregate created and read back through the API: command handler → Postgres → query handler.</summary>
public sealed class CreateThroughApiTests(PostgresDatabase database) : DatabaseTests(database)
{
    [Fact]
    public async Task An_aggregate_created_through_the_api_is_read_back()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = Api.CreateClient();

        using var created = await client.PostAsJsonAsync("/api/sample/orders", new { customerName = "Kari Nordmann" }, ct);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var id = createdBody.RootElement.GetProperty("id").GetGuid();
        Assert.Equal($"/api/sample/orders/{id}", created.Headers.Location?.AbsolutePath);

        using var read = await client.GetAsync(created.Headers.Location, ct);

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        using var readBody = await JsonDocument.ParseAsync(await read.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var order = readBody.RootElement;
        Assert.Equal(id, order.GetProperty("id").GetGuid());
        Assert.Equal("Kari Nordmann", order.GetProperty("customerName").GetString());
        Assert.Equal("draft", order.GetProperty("status").GetString());
        Assert.False(order.TryGetProperty("note", out _), "A null note is omitted.");
        Assert.NotEqual(0u, order.GetProperty("version").GetUInt32());
    }

    [Fact]
    public async Task An_unknown_aggregate_returns_404_with_its_code()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = Api.CreateClient();

        using var response = await client.GetAsync($"/api/sample/orders/{Guid.CreateVersion7()}", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        Assert.Equal("sample.order.not_found", body.RootElement.GetProperty("code").GetString());
    }
}
