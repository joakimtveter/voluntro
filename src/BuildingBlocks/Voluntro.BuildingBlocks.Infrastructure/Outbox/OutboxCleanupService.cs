using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Voluntro.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Runs <see cref="OutboxCleanup"/> when the API starts and then every hour. Added by
/// <see cref="OutboxServiceCollectionExtensions.AddOutboxProcessing"/>, in the API only.
/// </summary>
public sealed class OutboxCleanupService(
    OutboxCleanup cleanup,
    TimeProvider timeProvider,
    ILogger<OutboxCleanupService> logger) : BackgroundService
{
    /// <summary>How often the cleanup runs; retention is counted in days, so hourly is plenty.</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);

        try
        {
            do
            {
                try
                {
                    await cleanup.CleanUpAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
#pragma warning disable CA1031 // The database may be down for a while; the next run catches up.
                catch (Exception exception)
#pragma warning restore CA1031
                {
                    OutboxLog.CleanupFailed(logger, exception);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Stopping while waiting for the next run.
        }
    }
}
