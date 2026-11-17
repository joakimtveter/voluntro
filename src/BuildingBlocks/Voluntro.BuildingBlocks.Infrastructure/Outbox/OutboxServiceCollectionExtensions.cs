using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Voluntro.BuildingBlocks.Infrastructure.Outbox;

public static class OutboxServiceCollectionExtensions
{
    /// <summary>
    /// Starts delivering every module's outbox: adds <see cref="OutboxProcessingService"/>, which runs the processor
    /// <c>AddModuleDbContext</c> registered for each module context, and <see cref="OutboxCleanupService"/>, which deletes
    /// old messages and inbox records hourly; and validates <see cref="OutboxOptions"/> (section
    /// <c>Outbox</c>) at startup. Called by the API only, after its modules: modules never start background work
    /// themselves, so the MigrationService, which registers the same modules, never delivers anything (ADR-0014).
    /// </summary>
    public static IServiceCollection AddOutboxProcessing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<OutboxOptions>()
            .BindConfiguration(OutboxOptions.SectionName)
            .Validate(
                options => !options.Validate().Any(),
                $"Invalid {OutboxOptions.SectionName} settings: MaxAttempts and BatchSize must be at least 1, and RetentionPeriod and PollingInterval positive.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<OutboxProcessingService>();

        // The work and the hourly loop are separate, so tests can run the work without the loop.
        services.AddSingleton<OutboxCleanup>();
        services.AddHostedService<OutboxCleanupService>();

        return services;
    }
}
