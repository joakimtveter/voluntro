using System.Net;
using System.Text.Json;
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

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken ct) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

    /// <summary>The W3C trace ID alone: 32 lowercase hex characters, not the full "00-…-…-01" traceparent.</summary>
    [GeneratedRegex("^[0-9a-f]{32}$")]
    private static partial Regex BareTraceId();
}
