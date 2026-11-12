using System.Net;
using System.Text.Json;
using Voluntro.Api.IntegrationTests.Sample;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// The paged-list contract: <c>?page&amp;pageSize&amp;sort&amp;search</c> in, <c>{ items, page, pageSize, totalCount }</c> out.
/// Out-of-range input is clamped rather than rejected, so a handler never sees an invalid page.
/// </summary>
public sealed class PagingTests(VoluntroApiFactory factory) : IClassFixture<VoluntroApiFactory>
{
    private static int TotalItems => SampleItems.All.Count;

    [Fact]
    public async Task A_paged_list_has_exactly_the_agreed_shape()
    {
        using var body = await GetItemsAsync("?page=2&pageSize=10");
        var root = body.RootElement;

        Assert.Equal(
            ["items", "page", "pageSize", "totalCount"],
            root.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
        Assert.Equal(2, root.GetProperty("page").GetInt32());
        Assert.Equal(10, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(TotalItems, root.GetProperty("totalCount").GetInt32());

        // Page 2 of 10 holds items 11-20.
        var ids = root.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid());
        Assert.Equal(SampleItems.All.Skip(10).Take(10).Select(item => item.Id), ids);
    }

    [Fact]
    public async Task Without_parameters_the_first_page_of_25_is_returned()
    {
        using var body = await GetItemsAsync("");
        var root = body.RootElement;

        Assert.Equal(1, root.GetProperty("page").GetInt32());
        Assert.Equal(25, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(25, root.GetProperty("items").GetArrayLength());
        Assert.Equal(TotalItems, root.GetProperty("totalCount").GetInt32());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1, 1)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    [InlineData(1000, 100)]
    public async Task Page_size_is_clamped_to_between_1_and_100(int requested, int expected)
    {
        using var body = await GetItemsAsync($"?pageSize={requested}");

        Assert.Equal(expected, body.RootElement.GetProperty("pageSize").GetInt32());
        Assert.Equal(Math.Min(expected, TotalItems), body.RootElement.GetProperty("items").GetArrayLength());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task A_page_below_1_is_clamped_to_1(int requested)
    {
        using var body = await GetItemsAsync($"?page={requested}&pageSize=5");

        Assert.Equal(1, body.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(SampleItems.First.Id, body.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task A_page_past_the_end_is_empty_but_keeps_the_total()
    {
        using var body = await GetItemsAsync("?page=99");

        Assert.Equal(99, body.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(0, body.RootElement.GetProperty("items").GetArrayLength());
        Assert.Equal(TotalItems, body.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Search_reaches_the_handler()
    {
        using var body = await GetItemsAsync("?search=second");

        var item = Assert.Single(body.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(SampleItems.Second.Id, item.GetProperty("id").GetGuid());
        Assert.Equal(1, body.RootElement.GetProperty("totalCount").GetInt32());
    }

    private async Task<JsonDocument> GetItemsAsync(string query)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/sample/items{query}", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }
}
