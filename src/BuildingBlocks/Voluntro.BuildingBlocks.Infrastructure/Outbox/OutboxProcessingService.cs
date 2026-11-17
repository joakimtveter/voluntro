using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Voluntro.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Runs every module's <see cref="IOutboxProcessor"/> on a timer (every <see cref="OutboxOptions.PollingInterval"/>),
/// side by side, so a slow consumer in one module doesn't hold up another's outbox. Added by
/// <see cref="OutboxServiceCollectionExtensions.AddOutboxProcessing"/>, in the API only.
/// </summary>
public sealed class OutboxProcessingService(
    IEnumerable<IOutboxProcessor> processors,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessingService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(processors.Select(processor => RunAsync(processor, stoppingToken)));

    private async Task RunAsync(IOutboxProcessor processor, CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollingInterval, timeProvider);

        try
        {
            do
            {
                try
                {
                    await processor.ProcessPendingAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
#pragma warning disable CA1031 // The database may be down for a while; the loop must outlive it.
                catch (Exception exception)
#pragma warning restore CA1031
                {
                    OutboxLog.ProcessingFailed(logger, processor.ContextType.Name, exception);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Stopping while waiting for the next tick.
        }
    }
}
