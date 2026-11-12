using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.BuildingBlocks.Infrastructure;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// A disabled module's endpoints answer 404, exactly like a route that doesn't exist, before anything else
/// (model binding, the action, later authentication) gets to look at the request.
/// </summary>
public sealed class ModuleGateTests(VoluntroApiFactory factory) : IClassFixture<VoluntroApiFactory>
{
    private const string ProblemJson = "application/problem+json";

    private static string KnownItemUrl => $"/api/sample/items/{SampleItems.First.Id}";

    [Fact]
    public async Task A_disabled_modules_endpoint_returns_404()
    {
        var ct = TestContext.Current.CancellationToken;
        using var disabled = WithSampleModuleDisabled();
        using var client = disabled.CreateClient();

        using var response = await client.GetAsync(KnownItemUrl, ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task An_unknown_route_returns_404_problem_details()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/does-not-exist", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct))!.AsObject();
        Assert.Equal(404, (int)body["status"]!);
        Assert.NotNull(body["traceId"]);
        Assert.False(body.ContainsKey("code"), "A framework-produced 404 has no error code.");
    }

    [Fact]
    public async Task A_disabled_module_looks_exactly_like_an_unknown_route()
    {
        var ct = TestContext.Current.CancellationToken;
        using var disabled = WithSampleModuleDisabled();
        using var client = disabled.CreateClient();

        using var gated = await client.GetAsync(KnownItemUrl, ct);
        using var unknown = await client.GetAsync("/api/does-not-exist", ct);

        Assert.Equal(unknown.StatusCode, gated.StatusCode);
        Assert.Equal(unknown.Content.Headers.ContentType?.MediaType, gated.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            WithoutTraceId(await unknown.Content.ReadAsStringAsync(ct)),
            WithoutTraceId(await gated.Content.ReadAsStringAsync(ct)));
    }

    [Fact]
    public async Task Every_controller_of_a_disabled_module_is_gated_before_its_action_runs()
    {
        var ct = TestContext.Current.CancellationToken;
        using var disabled = WithSampleModuleDisabled();
        using var client = disabled.CreateClient();

        // Enabled, this action throws and the response is a 500 (see ProblemDetailsTests).
        using var response = await client.GetAsync("/api/sample/boom", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_disabled_module_is_gated_before_model_binding()
    {
        var ct = TestContext.Current.CancellationToken;
        const string invalidRequest = "/api/sample/errors/NotAnErrorType";

        using var enabledClient = factory.CreateClient();
        using var whenEnabled = await enabledClient.GetAsync(invalidRequest, ct);

        using var disabled = WithSampleModuleDisabled();
        using var disabledClient = disabled.CreateClient();
        using var whenDisabled = await disabledClient.GetAsync(invalidRequest, ct);

        // Enabled, [ApiController] rejects the unbindable route value with a 400 that names the parameter.
        // Disabled, the request must never get that far: a 400 would reveal the endpoint exists.
        Assert.Equal(HttpStatusCode.BadRequest, whenEnabled.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, whenDisabled.StatusCode);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task A_disabled_module_looks_like_an_unknown_route_for_a_method_its_route_does_not_support(string method)
    {
        var ct = TestContext.Current.CancellationToken;
        const string getOnlyRoute = "/api/sample/items";

        // Enabled, routing itself answers 405 with an Allow header: no controller action is involved.
        using var enabledClient = factory.CreateClient();
        using var whenEnabled = await enabledClient.SendAsync(new HttpRequestMessage(new HttpMethod(method), getOnlyRoute), ct);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, whenEnabled.StatusCode);

        using var disabled = WithSampleModuleDisabled();
        using var client = disabled.CreateClient();
        using var gated = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), getOnlyRoute), ct);
        using var unknown = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/api/does-not-exist"), ct);

        // Disabled, a 405 (or its Allow header) would reveal that the route exists.
        Assert.Equal(HttpStatusCode.NotFound, gated.StatusCode);
        Assert.Empty(gated.Content.Headers.Allow);
        Assert.Equal(unknown.Content.Headers.ContentType?.MediaType, gated.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            WithoutTraceId(await unknown.Content.ReadAsStringAsync(ct)),
            WithoutTraceId(await gated.Content.ReadAsStringAsync(ct)));
    }

    private WebApplicationFactory<Program> WithSampleModuleDisabled() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IModuleActivation>(new DisabledModules(new SampleModule().Key))));

    /// <summary>The trace ID differs per request by design; everything else in the body must match.</summary>
    private static string WithoutTraceId(string json)
    {
        var body = JsonNode.Parse(json)!.AsObject();
        body.Remove("traceId");
        return body.ToJsonString();
    }
}
