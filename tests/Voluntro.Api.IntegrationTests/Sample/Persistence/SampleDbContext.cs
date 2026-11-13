using Microsoft.EntityFrameworkCore;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;

namespace Voluntro.Api.IntegrationTests.Sample.Persistence;

/// <summary>
/// The sample module's DbContext. Schema, naming and conventions all come from <see cref="ModuleDbContext"/>;
/// the module only names its schema and applies its entity configurations.
/// </summary>
public sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : ModuleDbContext(options), ISampleDbContext
{
    public DbSet<SampleOrder> Orders => Set<SampleOrder>();

    protected override string Schema => SampleModule.ModuleKey;

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SampleDbContext).Assembly);
}
