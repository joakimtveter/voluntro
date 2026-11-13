using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Voluntro.AppHost.Tests;

/// <summary>
/// The whole AppHost (Postgres container, MigrationService and API), started once for the smoke tests and ready
/// when the API is healthy. Needs Docker or Podman running.
/// </summary>
public sealed class RunningAppHost : IAsyncLifetime
{
    // Covers pulling the Postgres image on a cold machine.
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    private DistributedApplication? _app;

    public DistributedApplication App => _app ?? throw new InvalidOperationException("The AppHost has not been started.");

    public async ValueTask InitializeAsync()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(StartupTimeout);

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Voluntro_AppHost>(timeout.Token);
        _app = await appHost.BuildAsync(timeout.Token);
        await _app.StartAsync(timeout.Token);

        await _app.ResourceNotifications.WaitForResourceHealthyAsync("api", timeout.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
