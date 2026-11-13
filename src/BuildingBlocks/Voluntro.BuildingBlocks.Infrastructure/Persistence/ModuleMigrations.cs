using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Voluntro.BuildingBlocks.Infrastructure.Persistence;

public static class ModuleMigrations
{
    /// <summary>
    /// Applies the migrations of every DbContext registered with
    /// <see cref="ModuleDbContextServiceCollectionExtensions.AddModuleDbContext{TContext}"/>, in registration order
    /// (the order of <c>AddModules</c>). Each module migrates on its own, with no transaction across modules: there
    /// are no foreign keys across schemas, and after a partial failure the next run carries on.
    /// </summary>
    /// <remarks>
    /// Called by <c>Voluntro.MigrationService</c> and by the integration-test harness, never by the API.
    /// EF throws if a model has changes that no migration covers, which catches a forgotten migration.
    /// </remarks>
    public static async Task MigrateModulesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (var registration in services.GetServices<ModuleDbContextRegistration>())
        {
            await using var scope = services.CreateAsyncScope();
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(registration.ContextType);

            await context.Database.MigrateAsync(cancellationToken);
        }
    }
}
