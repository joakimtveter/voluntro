using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Voluntro.Api.Options;
using Voluntro.BuildingBlocks.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDataSource("voluntro");

// Options are validated when the app starts, so a missing or invalid setting stops it straight away.
builder.Services.AddOptions<AppOptions>()
    .BindConfiguration(AppOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AppOptions>, AppOptionsValidator>();

// Add services to the container.

builder.Services.AddControllers();

// Every module is listed here explicitly. Platform, Identity, ... are added as they are built.
builder.AddModules();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.

// Return the request's trace ID with every response, so an error report can be found in the dashboard.
// OnStarting runs just before the headers are sent, so the header survives an exception handler clearing the response.
app.Use(async (context, next) =>
{
    var traceId = context.Features.Get<IHttpActivityFeature>()?.Activity.TraceId.ToString()
        ?? context.TraceIdentifier;

    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Trace-Id"] = traceId;
        return Task.CompletedTask;
    });

    await next(context);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
