using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;

namespace Voluntro.Api.IntegrationTests.Database;

/// <summary>
/// A real Postgres for the database tests, never the EF in-memory provider. Started once for the
/// <see cref="DatabaseCollection"/>, migrated through <see cref="ModuleMigrations.MigrateModulesAsync"/> (the
/// MigrationService's code path), and emptied before each test by <see cref="DatabaseTests"/>.
/// </summary>
/// <remarks>
/// Needs Docker (or Podman). Once a second database collection exists, move the container to an assembly fixture
/// and give each collection its own database.
/// </remarks>
public sealed class PostgresDatabase : IAsyncLifetime
{
    // The image Aspire 13.6.5 runs in the AppHost; keep them in step.
    private const string Image = "postgres:18.3";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(Image)
        .WithDatabase("voluntro")
        .Build();

    private VoluntroApiFactory? _api;
    private string? _truncateSql;

    /// <summary>The API, connected to this database.</summary>
    public VoluntroApiFactory Api => _api ?? throw new InvalidOperationException("The database has not been started.");

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _container.StartAsync(cancellationToken);

        _api = new VoluntroApiFactory { ConnectionString = _container.GetConnectionString() };
        await _api.Services.MigrateModulesAsync(cancellationToken);

        // The tables don't change after migrating, so the statement is built once.
        _truncateSql = await BuildTruncateSqlAsync(cancellationToken);
    }

    /// <summary>Empties every module table; the migrations history stays.</summary>
    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        if (_truncateSql is null)
        {
            return;
        }

        await using var command = DataSource.CreateCommand(_truncateSql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_api is not null)
        {
            await _api.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    private NpgsqlDataSource DataSource => Api.Services.GetRequiredService<NpgsqlDataSource>();

    private async Task<string?> BuildTruncateSqlAsync(CancellationToken cancellationToken)
    {
        // Every table in the module schemas except each schema's migrations history. %I quotes the names.
        const string ListTables = """
            SELECT format('%I.%I', schemaname, tablename)
            FROM pg_tables
            WHERE schemaname NOT IN ('pg_catalog', 'information_schema') AND tablename <> $1
            ORDER BY 1
            """;

        await using var command = DataSource.CreateCommand(ListTables);
        command.Parameters.Add(new NpgsqlParameter { Value = HistoryRepository.DefaultTableName });

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        // CASCADE covers foreign keys between the tables; RESTART IDENTITY resets any sequences.
        return tables.Count == 0 ? null : $"TRUNCATE TABLE {string.Join(", ", tables)} RESTART IDENTITY CASCADE";
    }
}
