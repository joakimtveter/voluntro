using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Voluntro.Api.IntegrationTests.Database;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.BuildingBlocks.Infrastructure;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// Hosts Voluntro.Api in memory, with the test-only <see cref="SampleModule"/> registered
/// through the same <c>AddModule</c> path that <c>Program.cs</c> uses for real modules.
/// </summary>
public sealed class VoluntroApiFactory : WebApplicationFactory<Program>
{
    // Npgsql only connects when a query runs, so tests that never touch the database don't need it to exist.
    private const string UnusedDatabase = "Host=localhost;Database=voluntro_tests;Username=test;Password=test";

    /// <summary>
    /// The database the API connects to. The default is never connected to; <see cref="PostgresDatabase"/>
    /// points it at a real one.
    /// </summary>
    public string ConnectionString { get; init; } = UnusedDatabase;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Run as production does, so Development-only behaviour can't make a test pass by accident.
        builder.UseEnvironment(Environments.Production);

        // Settings Program.cs needs at startup. UseSetting values are visible while Program.cs runs.
        builder.UseSetting("App:PublicBaseUrl", "https://voluntro.test");
        builder.UseSetting("ConnectionStrings:voluntro", ConnectionString);

        // Runs after Program.cs has registered its own services.
        builder.ConfigureServices((context, services) =>
            services.AddModule(new SampleModule(), context.Configuration));
    }
}
