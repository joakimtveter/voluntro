using Voluntro.BuildingBlocks.Infrastructure;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;
using Voluntro.MigrationService;

// Applies every module's migrations, then exits. The API never migrates: Aspire's WaitForCompletion (and later
// compose's service_completed_successfully) starts it only after this exits with 0.
// A plain program rather than a BackgroundService, so a failure is simply exit code 1.
var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDataSource("voluntro");

// The same modules as Voluntro.Api, listed explicitly. A module missing here never gets its tables;
// one list shared by both hosts is an open question, decided with Platform.
builder.AddModules();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var stopping = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

try
{
    // Started so telemetry and logs reach the Aspire dashboard; Ctrl+C cancels the migrations.
    await host.StartAsync();
    await host.Services.MigrateModulesAsync(stopping);
    return 0;
}
catch (Exception exception)
{
    Log.MigrationsFailed(logger, exception);
    return 1;
}
finally
{
    await host.StopAsync();
}
