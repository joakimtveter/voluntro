using System.Net;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// Enums are strings in both directions. In a route or query value that means the enum's name, in any casing;
/// the integer behind it is rejected, as it is in a JSON body.
/// </summary>
public sealed class EnumBindingTests(VoluntroApiFactory factory) : IClassFixture<VoluntroApiFactory>
{
    [Theory]
    [InlineData("/api/sample/errors/conflict")]
    [InlineData("/api/sample/errors/Conflict")]
    [InlineData("/api/sample/errors?type=conflict")]
    [InlineData("/api/sample/errors?type=Conflict")]
    public async Task An_enum_name_binds_in_any_casing(string url)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(url, ct);

        // The endpoint answers with the status of the error type it was given.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/sample/errors/3")]        // 3 is Conflict
    [InlineData("/api/sample/errors?type=3")]
    [InlineData("/api/sample/errors/+3")]
    [InlineData("/api/sample/errors?type=-1")]
    public async Task An_integer_is_rejected_as_an_enum_value(string url)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(url, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
