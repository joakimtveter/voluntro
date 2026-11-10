using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Voluntro.AppHost.Tests;

/// <summary>
/// Starts the whole AppHost (Postgres container and API) and checks the API is ready.
/// Needs Docker or Podman running.
/// </summary>
public sealed class SmokeTests
{
    // Covers pulling the Postgres image on a cold machine.
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    [Fact]
    public async Task Api_is_ready_when_the_AppHost_starts()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(StartupTimeout);

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Voluntro_AppHost>(timeout.Token);
        await using var app = await appHost.BuildAsync(timeout.Token);
        await app.StartAsync(timeout.Token);

        await app.ResourceNotifications.WaitForResourceHealthyAsync("api", timeout.Token);

        using var client = app.CreateHttpClient("api");
        using var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative), timeout.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
