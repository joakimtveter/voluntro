using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

namespace Voluntro.AppHost.Tests;

/// <summary>
/// The whole AppHost (Postgres container, MigrationService and API), started once for the smoke tests and ready
/// when the API is healthy, or as soon as the migrations have failed. Needs Docker or Podman running.
/// </summary>
public sealed class RunningAppHost : IAsyncLifetime
{
    // Covers pulling the Postgres image on a cold machine.
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    private DistributedApplication? _app;

    public DistributedApplication App => _app ?? throw new InvalidOperationException("The AppHost has not been started.");

    /// <summary>False when the migrations failed, because the API then never starts.</summary>
    public bool ApiIsHealthy { get; private set; }

    public async ValueTask InitializeAsync()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(StartupTimeout);

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Voluntro_AppHost>(timeout.Token);
        _app = await appHost.BuildAsync(timeout.Token);
        await _app.StartAsync(timeout.Token);

        // The migrations first. If they fail, the API never starts, and waiting for it would only run into the
        // timeout; the fixture stays usable instead, so the smoke test can report the exit code.
        var notifications = _app.ResourceNotifications;
        var migrations = await notifications.WaitForResourceAsync(
            "migrations",
            resource => resource.Snapshot.ExitCode is not null,
            timeout.Token);

        if (migrations.Snapshot.ExitCode == 0)
        {
            // Gives up at once if the API fails to start, rather than waiting out the timeout.
            await notifications.WaitForResourceHealthyAsync("api", WaitBehavior.StopOnResourceUnavailable, timeout.Token);
            ApiIsHealthy = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
