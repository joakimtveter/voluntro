using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Base class for every module's DbContext. Everything that shapes the model is set here, in sealed overrides, so the
/// running app and <c>dotnet ef</c> build the same model and a module's context can't forget to call <c>base</c>:
/// the module's own schema with its own migrations history, snake_case names, strongly-typed IDs stored as
/// <c>uuid</c>, enums stored as text, and <see cref="AggregateRoot{TId}.Version"/> on Postgres <c>xmin</c>.
/// </summary>
/// <remarks>
/// Register a context with <see cref="ModuleDbContextServiceCollectionExtensions.AddModuleDbContext{TContext}"/>,
/// which only supplies the connection. Migrations are applied by <see cref="ModuleMigrations.MigrateModulesAsync"/>,
/// never by the API.
/// </remarks>
public abstract class ModuleDbContext(DbContextOptions options) : DbContext(options)
{
    /// <summary>
    /// The module's schema: the same constant its <see cref="IModule.Key"/> returns, e.g. <c>"sample"</c>.
    /// Fixed per context type, because EF builds and caches one model per type.
    /// </summary>
    protected abstract string Schema { get; }

    /// <summary>Applies the module's entity configurations, e.g. with <c>ApplyConfigurationsFromAssembly</c>.</summary>
    protected abstract void ConfigureModel(ModelBuilder modelBuilder);

    protected sealed override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        // Registration (or the design-time factory) has already chosen the connection; this keeps it and adds to it.
        optionsBuilder
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schema))
            .UseSnakeCaseNamingConvention();
    }

    protected sealed override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        foreach (var idType in FindEntityIdTypes())
        {
            configurationBuilder
                .Properties(idType)
                .HaveConversion(typeof(EntityIdConverter<>).MakeGenericType(idType));
        }

        // Stored by member name (Published), not the API's camelCase: the database isn't the API.
        configurationBuilder.Properties<Enum>().HaveConversion<string>();

        // Real conventions rather than a loop at the end of OnModelCreating, so explicit configuration still wins.
        configurationBuilder.Conventions.Add(_ => new EntityIdKeyConvention());
        configurationBuilder.Conventions.Add(_ => new AggregateRootConvention());
    }

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        ConfigureModel(modelBuilder);
    }

    /// <summary>
    /// The ID types in the assemblies of this context's entity types, i.e. the module's Domain. Cross-module
    /// references are plain Guids, so a context never needs another module's ID types. Non-public properties are
    /// included, so <c>DbSet</c>s that only implement the module's narrow interface explicitly are found too.
    /// </summary>
    private IEnumerable<Type> FindEntityIdTypes() =>
        GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.PropertyType)
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(dbSet => dbSet.GetGenericArguments()[0].Assembly)
            .Distinct()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(EntityIds.IsEntityId);
}
