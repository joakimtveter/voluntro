using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.Api.IntegrationTests.Database;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.Api.IntegrationTests.Sample.Persistence;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// Every 400 for a malformed request has one shape, whether a validator or model binding found the problem:
/// <c>code</c> <c>common.validation_failed</c> and <c>errors: { field: [codes] }</c>, with no English text.
/// </summary>
public sealed class ValidationTests(PostgresDatabase database) : DatabaseTests(database)
{
    private const string Orders = "/api/sample/orders";

    [Fact]
    public async Task An_invalid_command_returns_its_field_errors_and_persists_nothing()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = Api.CreateClient();

        using var response = await client.PostAsJsonAsync(Orders, new { customerName = " " }, ct);

        await AssertValidationErrorsAsync(response, ("customerName", ["required"]));
        await using var scope = CreateScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<SampleDbContext>().Orders.CountAsync(ct));
    }

    [Fact]
    public async Task A_missing_property_is_left_to_the_validator()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = Api.CreateClient();

        using var response = await client.PostAsJsonAsync(Orders, new { note = "No name" }, ct);

        await AssertValidationErrorsAsync(response, ("customerName", ["required"]));
    }

    [Fact]
    public async Task A_value_over_the_limit_is_too_long()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = Api.CreateClient();
        var name = new string('x', CreateSampleOrderValidator.CustomerNameMaxLength + 1);

        using var response = await client.PostAsJsonAsync(Orders, new { customerName = name }, ct);

        await AssertValidationErrorsAsync(response, ("customerName", ["too_long"]));
    }

    [Fact]
    public async Task A_property_of_the_wrong_json_type_is_an_invalid_value()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = Api.CreateClient();

        using var response = await client.PostAsync(Orders, Json("""{ "customerName": 5 }"""), ct);

        await AssertValidationErrorsAsync(response, ("customerName", ["invalid_value"]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("null")]
    public async Task A_missing_or_unreadable_body_is_an_invalid_value_of_the_whole_body(string body)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = Api.CreateClient();

        using var response = await client.PostAsync(Orders, Json(body), ct);

        await AssertValidationErrorsAsync(response, ("$", ["invalid_value"]));
    }

    [Fact]
    public async Task An_unparsable_query_value_is_an_invalid_value_named_as_the_client_sent_it()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = Api.CreateClient();

        using var response = await client.GetAsync("/api/sample/items?page=abc", ct);

        await AssertValidationErrorsAsync(response, ("page", ["invalid_value"]));
    }

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private static async Task AssertValidationErrorsAsync(
        HttpResponseMessage response,
        params (string Field, string[] Codes)[] expected)
    {
        var ct = TestContext.Current.CancellationToken;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = body.RootElement;
        Assert.Equal("common.validation_failed", root.GetProperty("code").GetString());

        var errors = root.GetProperty("errors")
            .EnumerateObject()
            .Select(field => (field.Name, field.Value.EnumerateArray().Select(code => code.GetString()!).ToArray()))
            .ToArray();
        Assert.Equal(expected.Select(e => (e.Field, e.Codes)), errors);
    }
}
