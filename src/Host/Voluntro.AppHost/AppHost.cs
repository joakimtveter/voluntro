var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("voluntro");

var api = builder.AddProject<Projects.Voluntro_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health/ready");

// Until the web app exists, users reach Voluntro through the API's own address.
// Point this at the web endpoint once web is added to the AppHost.
api.WithEnvironment("App__PublicBaseUrl", api.GetEndpoint("http"));

builder.Build().Run();
