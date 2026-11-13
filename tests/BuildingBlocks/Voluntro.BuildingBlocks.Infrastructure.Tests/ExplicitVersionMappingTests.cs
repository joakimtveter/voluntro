using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// A module that configures an aggregate's <c>Version</c> itself owns the whole mapping: the convention adds nothing
/// to it. Mixing the two would give the module's column name with the convention's <c>xid</c> type, a real column
/// that neither EF nor Postgres writes. Building the model doesn't connect, so no database is needed.
/// </summary>
public sealed class ExplicitVersionMappingTests
{
    [Fact]
    public void The_convention_leaves_a_version_the_module_configured_alone()
    {
        var options = new DbContextOptionsBuilder<CounterDbContext>().UseNpgsql("Host=never-connected").Options;
        using var context = new CounterDbContext(options);

        var version = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(Counter))!
            .FindProperty(nameof(Counter.Version))!;

        Assert.Equal(CounterDbContext.VersionColumn, version.GetColumnName());
        Assert.NotEqual("xid", version.GetColumnType());
        Assert.False(version.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.Never, version.ValueGenerated);
    }

    private readonly record struct CounterId(Guid Value) : IEntityId<CounterId>
    {
        public static CounterId New() => new(Guid.CreateVersion7());

        public static CounterId From(Guid value) => new(value);
    }

    private sealed class Counter(CounterId id) : AggregateRoot<CounterId>(id);

    /// <summary>Sets only the column name of <c>Version</c>, the smallest explicit configuration there is.</summary>
    private sealed class CounterDbContext(DbContextOptions<CounterDbContext> options) : ModuleDbContext(options)
    {
        public const string VersionColumn = "row_version";

        public DbSet<Counter> Counters => Set<Counter>();

        protected override string Schema => "counter";

        protected override void ConfigureModel(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<Counter>().Property(counter => counter.Version).HasColumnName(VersionColumn);
    }
}
