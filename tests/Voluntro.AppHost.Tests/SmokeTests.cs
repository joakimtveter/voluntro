using System.Net;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

namespace Voluntro.AppHost.Tests;

public sealed class SmokeTests(RunningAppHost appHost) : IClassFixture<RunningAppHost>
{
    [Fact]
    public async Task Api_is_ready_when_the_AppHost_starts()
    {
        // A request to an API that never started would only wait out HttpClient's timeout.
        Assert.True(appHost.ApiIsHealthy, "The API never started, because the migrations failed.");

        var ct = TestContext.Current.CancellationToken;
        using var client = appHost.App.CreateHttpClient("api");

        using var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative), ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Migrations_finish_successfully_before_the_api_starts()
    {
        var notifications = appHost.App.ResourceNotifications;
        var migrations = await notifications.WaitForResourceAsync(
            "migrations",
            resource => resource.Snapshot.ExitCode is not null,
            TestContext.Current.CancellationToken);
        Assert.True(notifications.TryGetCurrentState("api", out var api));

        Assert.Equal(0, migrations.Snapshot.ExitCode);

        // Not just "WaitForCompletion is wired up": the API process really started after the migrations exited.
        var migrationsStopped = Assert.NotNull(migrations.Snapshot.StopTimeStamp);
        var apiStarted = Assert.NotNull(api.Snapshot.StartTimeStamp);
        Assert.True(apiStarted >= migrationsStopped, $"api started at {apiStarted:O}, migrations stopped at {migrationsStopped:O}.");
    }
}
