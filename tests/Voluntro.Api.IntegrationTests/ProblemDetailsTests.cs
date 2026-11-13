using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// Failures become RFC 9457 ProblemDetails: expected ones (a failed Result) with the module's error code,
/// unexpected ones (exceptions) as a generic 500 that reveals nothing.
/// </summary>
public sealed partial class ProblemDetailsTests(VoluntroApiFactory factory) : IClassFixture<VoluntroApiFactory>
{
    private const string ProblemJson = "application/problem+json";

    private static readonly Guid MissingItemId = Guid.Parse("01930000-0000-7000-8000-0000000000ff");

    /// <summary>The agreed mapping. A new ErrorType without an entry here fails <see cref="Every_error_type_has_an_expected_status"/>.</summary>
    private static readonly Dictionary<ErrorType, HttpStatusCode> ExpectedStatus = new()
    {
        [ErrorType.Failure] = HttpStatusCode.InternalServerError,
        [ErrorType.Validation] = HttpStatusCode.BadRequest,
        [ErrorType.NotFound] = HttpStatusCode.NotFound,
        [ErrorType.Conflict] = HttpStatusCode.Conflict,
        [ErrorType.Forbidden] = HttpStatusCode.Forbidden,
        [ErrorType.Unauthorized] = HttpStatusCode.Unauthorized,
    };

    public static TheoryData<ErrorType, HttpStatusCode> ErrorTypes
    {
        get
        {
            var data = new TheoryData<ErrorType, HttpStatusCode>();
            foreach (var (type, status) in ExpectedStatus)
            {
                data.Add(type, status);
            }

            return data;
        }
    }

    [Fact]
    public async Task A_missing_item_returns_404_problem_details_with_the_modules_error_code()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/sample/items/{MissingItemId}", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        using var body = await ReadJsonAsync(response, ct);
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("sample.item.not_found", body.RootElement.GetProperty("code").GetString());
        Assert.Contains(MissingItemId.ToString(), body.RootElement.GetProperty("detail").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Problem_details_carry_the_same_trace_id_as_the_response_header()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/sample/items/{MissingItemId}", ct);

        using var body = await ReadJsonAsync(response, ct);
        var traceId = body.RootElement.GetProperty("traceId").GetString();
        Assert.Matches(BareTraceId(), traceId);
        Assert.Equal(traceId, Assert.Single(response.Headers.GetValues("X-Trace-Id")));
    }

    [Theory]
    [MemberData(nameof(ErrorTypes))]
    public async Task Each_error_type_maps_to_its_http_status(ErrorType type, HttpStatusCode expected)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/sample/errors/{type}", ct);

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        using var body = await ReadJsonAsync(response, ct);
        Assert.Equal((int)expected, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(SampleDiagnosticsController.RequestedErrorCode, body.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public void Every_error_type_has_an_expected_status() =>
        Assert.Equal(Enum.GetValues<ErrorType>().Order(), ExpectedStatus.Keys.Order());

    [Fact]
    public async Task An_unhandled_exception_returns_a_generic_500_without_exception_details()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/sample/boom", ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        var raw = await response.Content.ReadAsStringAsync(ct);
        Assert.DoesNotContain(SampleDiagnosticsController.SecretExceptionMessage, raw, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(InvalidOperationException), raw, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(SampleDiagnosticsController), raw, StringComparison.Ordinal); // no stack trace

        using var body = JsonDocument.Parse(raw);
        Assert.Equal("common.unexpected", body.RootElement.GetProperty("code").GetString());
        Assert.Matches(BareTraceId(), body.RootElement.GetProperty("traceId").GetString());
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    public async Task A_framework_produced_error_is_problem_details_whatever_the_client_accepts(string accept)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/does-not-exist");
        request.Headers.Accept.ParseAdd(accept);
        using var response = await client.SendAsync(request, ct);
        using var withoutAccept = await client.GetAsync("/api/does-not-exist", ct);

        // The API has one error format. A client that doesn't ask for JSON still gets it, not a text/plain line.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct))!.AsObject();
        var traceId = (string?)body["traceId"];
        Assert.Matches(BareTraceId(), traceId);
        Assert.Equal(traceId, Assert.Single(response.Headers.GetValues("X-Trace-Id")));

        // Apart from the trace ID, the body is the one every other client gets.
        var expected = JsonNode.Parse(await withoutAccept.Content.ReadAsStringAsync(ct))!.AsObject();
        body.Remove("traceId");
        expected.Remove("traceId");
        Assert.Equal(expected.ToJsonString(), body.ToJsonString());
    }

    [Fact]
    public async Task A_405_from_routing_is_problem_details_whatever_the_client_accepts()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/sample/items");
        request.Headers.Accept.ParseAdd("text/html");
        using var response = await client.SendAsync(request, ct);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        using var body = await ReadJsonAsync(response, ct);
        Assert.Equal(405, body.RootElement.GetProperty("status").GetInt32());
        Assert.Matches(BareTraceId(), body.RootElement.GetProperty("traceId").GetString());
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken ct) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

    /// <summary>The W3C trace ID alone: 32 lowercase hex characters, not the full "00-…-…-01" traceparent.</summary>
    [GeneratedRegex("^[0-9a-f]{32}$")]
    private static partial Regex BareTraceId();
}
