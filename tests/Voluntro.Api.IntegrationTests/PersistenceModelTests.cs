using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.Api.IntegrationTests.Sample.Persistence;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// The conventions from <c>ModuleDbContext</c>, checked on the sample module's model. The context is resolved from
/// the API's own services, as a request would get it; building the model doesn't connect, so no database is needed.
/// </summary>
public sealed class PersistenceModelTests(VoluntroApiFactory factory) : IClassFixture<VoluntroApiFactory>
{
    [Fact]
    public void The_module_tables_live_in_the_module_schema()
    {
        WithContext(context =>
        {
            var model = DesignTimeModel(context);

            Assert.All(model.GetEntityTypes(), entityType => Assert.Equal(SampleModule.ModuleKey, entityType.GetSchema()));
        });
    }

    [Fact]
    public void The_migrations_history_table_is_in_the_module_schema()
    {
        WithContext(context =>
        {
            var relational = RelationalOptionsExtension.Extract(context.GetService<IDbContextOptions>());

            Assert.Equal(SampleModule.ModuleKey, relational.MigrationsHistoryTableSchema);
            Assert.Equal(HistoryRepository.DefaultTableName, relational.MigrationsHistoryTableName);
        });
    }

    [Fact]
    public void Names_are_snake_case()
    {
        WithContext(context =>
        {
            var model = DesignTimeModel(context);
            var order = model.FindEntityType(typeof(SampleOrder))!;
            var line = model.FindEntityType(typeof(SampleOrderLine))!;

            Assert.Equal("customer_name", order.FindProperty(nameof(SampleOrder.CustomerName))!.GetColumnName());
            Assert.Equal("order_id", line.FindProperty(nameof(SampleOrderLine.OrderId))!.GetColumnName());
            Assert.Equal("pk_order_lines", line.FindPrimaryKey()!.GetName());
        });
    }

    [Theory]
    [InlineData(typeof(SampleOrder), nameof(SampleOrder.Id))]
    [InlineData(typeof(SampleOrderLine), nameof(SampleOrderLine.Id))]
    [InlineData(typeof(SampleOrderLine), nameof(SampleOrderLine.OrderId))]
    public void Strongly_typed_ids_are_stored_as_uuid(Type entity, string propertyName)
    {
        WithContext(context =>
        {
            var property = DesignTimeModel(context).FindEntityType(entity)!.FindProperty(propertyName)!;

            Assert.Equal(typeof(Guid), property.GetValueConverter()?.ProviderClrType);
            Assert.Equal("uuid", property.GetColumnType());
        });
    }

    [Theory]
    [InlineData(typeof(SampleOrder))]
    [InlineData(typeof(SampleOrderLine))]
    public void Strongly_typed_id_keys_are_never_generated(Type entity)
    {
        WithContext(context =>
        {
            var key = DesignTimeModel(context).FindEntityType(entity)!.FindPrimaryKey()!;

            Assert.Equal(ValueGenerated.Never, Assert.Single(key.Properties).ValueGenerated);
        });
    }

    [Fact]
    public void An_aggregate_version_is_the_xmin_row_version()
    {
        WithContext(context =>
        {
            var version = DesignTimeModel(context).FindEntityType(typeof(SampleOrder))!.FindProperty(nameof(SampleOrder.Version))!;

            Assert.True(version.IsConcurrencyToken);
            Assert.Equal(ValueGenerated.OnAddOrUpdate, version.ValueGenerated);
            Assert.Equal("xmin", version.GetColumnName());
            Assert.Equal("xid", version.GetColumnType());
        });
    }

    [Fact]
    public void Domain_events_are_not_mapped()
    {
        WithContext(context =>
        {
            var order = DesignTimeModel(context).FindEntityType(typeof(SampleOrder))!;

            Assert.Null(order.FindProperty(nameof(SampleOrder.DomainEvents)));
            Assert.Null(order.FindNavigation(nameof(SampleOrder.DomainEvents)));
        });
    }

    [Fact]
    public void Enums_are_stored_as_text()
    {
        WithContext(context =>
        {
            var status = DesignTimeModel(context).FindEntityType(typeof(SampleOrder))!.FindProperty(nameof(SampleOrder.Status))!;

            // HaveConversion<string>() sets only the provider type; EF picks the enum-to-string converter
            // into the type mapping, so GetValueConverter() stays null.
            Assert.Equal(typeof(string), status.GetTypeMapping().Converter?.ProviderClrType);
            Assert.Equal("text", status.GetColumnType());
        });
    }

    // The migrations were generated through the design-time factory; this context comes from the API's services.
    // No pending changes means both build the same model, and nobody forgot a migration.
    [Fact]
    public void The_runtime_model_matches_the_migrations()
    {
        WithContext(context => Assert.False(context.Database.HasPendingModelChanges()));
    }

    // The design-time model keeps every annotation (column types, constraint names); the runtime model drops some.
    private static IModel DesignTimeModel(SampleDbContext context) => context.GetService<IDesignTimeModel>().Model;

    private void WithContext(Action<SampleDbContext> assert)
    {
        using var scope = factory.Services.CreateScope();
        assert(scope.ServiceProvider.GetRequiredService<SampleDbContext>());
    }
}
