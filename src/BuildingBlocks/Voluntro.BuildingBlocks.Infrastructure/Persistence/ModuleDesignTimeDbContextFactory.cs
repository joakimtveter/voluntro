using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Voluntro.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> create a module's DbContext without starting a host. Each module derives one in its
/// Infrastructure project, next to the context:
/// <code>
/// public sealed class PersonDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory&lt;MembershipDbContext&gt;
/// {
///     protected override MembershipDbContext Create(DbContextOptions&lt;MembershipDbContext&gt; options) => new(options);
/// }
/// </code>
/// </summary>
/// <remarks>
/// The model comes entirely from <see cref="ModuleDbContext"/>, so it matches the running app's. The connection
/// string is a placeholder: <c>migrations add</c> and <c>migrations script</c> never connect. Migrations are applied
/// by the MigrationService, not with <c>dotnet ef database update</c>.
/// </remarks>
/// <typeparam name="TContext">The module's DbContext.</typeparam>
public abstract class ModuleDesignTimeDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : ModuleDbContext
{
    private const string PlaceholderConnectionString = "Host=design-time-only";

    public TContext CreateDbContext(string[] args) =>
        Create(new DbContextOptionsBuilder<TContext>().UseNpgsql(PlaceholderConnectionString).Options);

    /// <summary>Constructs the context; usually <c>new(options)</c>.</summary>
    protected abstract TContext Create(DbContextOptions<TContext> options);
}
