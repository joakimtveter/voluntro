using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Voluntro.BuildingBlocks.Infrastructure.Persistence;

public static class ModuleDbContextServiceCollectionExtensions
{
    /// <summary>
    /// Registers a module's DbContext, scoped, and records it so <see cref="ModuleMigrations.MigrateModulesAsync"/>
    /// migrates it. Called from <see cref="IModule.Register"/>.
    /// </summary>
    /// <remarks>
    /// Only the connection is chosen here: the <see cref="NpgsqlDataSource"/> the host registers with
    /// <c>AddNpgsqlDataSource("voluntro")</c>. The model is set by <see cref="ModuleDbContext"/> alone.
    /// Transient failures are retried (ADR-0012). Each command is a single <c>SaveChangesAsync</c> with no explicit
    /// transaction, so the strategy retries one query or one save at a time and never re-runs a handler. An explicit
    /// transaction would have to be wrapped in <c>CreateExecutionStrategy().ExecuteAsync</c>.
    /// Never enable sensitive data logging here: it logs parameter values, i.e. personal data.
    /// </remarks>
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services)
        where TContext : ModuleDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<TContext>((provider, options) =>
            options.UseNpgsql(
                provider.GetRequiredService<NpgsqlDataSource>(),
                npgsql => npgsql.EnableRetryOnFailure()));

        services.AddSingleton(new ModuleDbContextRegistration(typeof(TContext)));

        return services;
    }
}

/// <summary>A module DbContext registered with <c>AddModuleDbContext</c>, in registration order.</summary>
internal sealed record ModuleDbContextRegistration(Type ContextType);
