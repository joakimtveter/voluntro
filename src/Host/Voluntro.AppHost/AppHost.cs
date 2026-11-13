var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("voluntro");

// Applies every module's migrations, then exits. The API never migrates.
var migrations = builder.AddProject<Projects.Voluntro_MigrationService>("migrations")
    .WithReference(database)
    .WaitFor(database);

// Starts only after the migrations have finished with exit code 0.
var api = builder.AddProject<Projects.Voluntro_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health/ready");

// Until the web app exists, users reach Voluntro through the API's own address.
// Point this at the web endpoint once web is added to the AppHost.
api.WithEnvironment("App__PublicBaseUrl", api.GetEndpoint("http"));

builder.Build().Run();
