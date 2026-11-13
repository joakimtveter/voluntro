using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// Developer tooling (the OpenAPI document and Swagger UI) exists only in Development,
/// and the API leaves HTTPS to the reverse proxy (Caddy) in every environment.
/// </summary>
public sealed class EnvironmentTests(VoluntroApiFactory factory) : IClassFixture<VoluntroApiFactory>
{
    private const string SwaggerUi = "/swagger";
    private const string OpenApiDocument = "/openapi/v1.json";

    [Fact]
    public async Task Swagger_UI_is_served_in_Development()
    {
        var ct = TestContext.Current.CancellationToken;
        using var development = InEnvironment(Environments.Development);
        using var client = development.CreateClient(); // follows the redirect from /swagger to its index page

        using var response = await client.GetAsync(SwaggerUi, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("swagger-ui", await response.Content.ReadAsStringAsync(ct), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_OpenAPI_document_is_served_in_Development()
    {
        var ct = TestContext.Current.CancellationToken;
        using var development = InEnvironment(Environments.Development);
        using var client = development.CreateClient();

        using var response = await client.GetAsync(OpenApiDocument, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        Assert.True(document.RootElement.TryGetProperty("openapi", out _), "Not an OpenAPI document.");
    }

    [Theory]
    [InlineData("Production", SwaggerUi)]
    [InlineData("Production", OpenApiDocument)]
    [InlineData("Staging", SwaggerUi)]
    [InlineData("Staging", OpenApiDocument)]
    public async Task Developer_tooling_is_absent_outside_Development(string environment, string path)
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = InEnvironment(environment);
        using var client = host.CreateClient();

        using var response = await client.GetAsync(path, ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_OpenAPI_document_describes_enums_as_camelCase_strings()
    {
        var ct = TestContext.Current.CancellationToken;
        using var development = InEnvironment(Environments.Development);
        using var client = development.CreateClient();

        using var response = await client.GetAsync(OpenApiDocument, ct);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        // Must agree with the responses (see JsonConventionsTests): the frontend's generated types come from here.
        var status = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("SampleItemStatus");
        var values = status.GetProperty("enum").EnumerateArray().Select(value => value.GetString());
        Assert.Equal(["draft", "published"], values.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task The_OpenAPI_document_names_paging_query_parameters_in_camelCase()
    {
        var ct = TestContext.Current.CancellationToken;
        using var development = InEnvironment(Environments.Development);
        using var client = development.CreateClient();

        using var response = await client.GetAsync(OpenApiDocument, ct);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        // The documented contract is ?page&pageSize&sort&search. Binding ignores case, so only the document
        // can get this wrong, and generated clients send exactly the names it lists.
        var list = document.RootElement.GetProperty("paths").GetProperty("/api/sample/items").GetProperty("get");
        var queryParameters = list.GetProperty("parameters").EnumerateArray()
            .Where(parameter => parameter.GetProperty("in").GetString() == "query")
            .Select(parameter => parameter.GetProperty("name").GetString());
        Assert.Equal(["page", "pageSize", "search", "sort"], queryParameters.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task The_OpenAPI_document_describes_both_the_success_and_the_problem_response_of_an_action()
    {
        var ct = TestContext.Current.CancellationToken;
        using var development = InEnvironment(Environments.Development);
        using var client = development.CreateClient();

        using var response = await client.GetAsync(OpenApiDocument, ct);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        // The frontend's generated types need the error shape as well as the DTO.
        var responses = document.RootElement.GetProperty("paths").GetProperty("/api/sample/items/{id}")
            .GetProperty("get").GetProperty("responses");
        Assert.Equal(["200", "404"], responses.EnumerateObject().Select(status => status.Name).Order(StringComparer.Ordinal));

        var ok = responses.GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Assert.EndsWith("/SampleItemDto", ok.GetProperty("$ref").GetString(), StringComparison.Ordinal);

        var notFound = responses.GetProperty("404").GetProperty("content").GetProperty("application/problem+json").GetProperty("schema");
        Assert.EndsWith("/ProblemDetails", notFound.GetProperty("$ref").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_OpenAPI_document_describes_a_create_as_201_with_the_new_id()
    {
        var ct = TestContext.Current.CancellationToken;
        using var development = InEnvironment(Environments.Development);
        using var client = development.CreateClient();

        using var response = await client.GetAsync(OpenApiDocument, ct);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var responses = document.RootElement.GetProperty("paths").GetProperty("/api/sample/orders")
            .GetProperty("post").GetProperty("responses");
        Assert.Equal(["201"], responses.EnumerateObject().Select(status => status.Name));

        var created = responses.GetProperty("201").GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Assert.EndsWith("/CreatedResponse", created.GetProperty("$ref").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Http_requests_are_not_redirected_to_https()
    {
        var ct = TestContext.Current.CancellationToken;

        // With an HTTPS port known, UseHttpsRedirection would answer 307. Caddy terminates TLS and talks
        // plain HTTP to the API, so the API must serve the request itself.
        using var host = factory.WithWebHostBuilder(builder => builder.UseSetting("https_port", "443"));
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync($"/api/sample/items/{Sample.SampleItems.First.Id}", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private WebApplicationFactory<Program> InEnvironment(string environment) =>
        factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment));
}
