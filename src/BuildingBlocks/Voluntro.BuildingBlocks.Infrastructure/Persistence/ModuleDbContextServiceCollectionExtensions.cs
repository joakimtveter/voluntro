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
    /// There is no retrying execution strategy: it rejects explicit transactions unless the whole unit is wrapped
    /// in <c>CreateExecutionStrategy().ExecuteAsync</c>, and a retry re-runs the handler. The transaction
    /// decorator decides that. Never enable sensitive data logging here: it logs parameter values, i.e. personal data.
    /// </remarks>
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services)
        where TContext : ModuleDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<TContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>()));

        services.AddSingleton(new ModuleDbContextRegistration(typeof(TContext)));

        return services;
    }
}

/// <summary>A module DbContext registered with <c>AddModuleDbContext</c>, in registration order.</summary>
internal sealed record ModuleDbContextRegistration(Type ContextType);
