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

// camelCase, enums as camelCase strings, nulls omitted: for controllers, ProblemDetails and the OpenAPI document.
builder.Services.AddApiJsonConventions();

// Every error response is an RFC 9457 ProblemDetails with the request's traceId.
builder.Services.AddApiProblemDetails();

// Every module is listed here explicitly. Platform, Identity, ... are added as they are built.
builder.AddModules();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.

// First, so it catches exceptions from everything after it: an unhandled exception is a bug and becomes
// a generic 500 ProblemDetails (code common.unexpected) in every environment; the exception is logged.
app.UseExceptionHandler();

// Gives every bodyless error response (unknown route, disabled module) a ProblemDetails body.
app.UseStatusCodePages();

// Return the request's trace ID with every response, so an error report can be found in the dashboard.
// OnStarting runs just before the headers are sent, so the header survives an exception handler clearing the response.
app.Use(async (context, next) =>
{
    var traceId = context.GetTraceId();

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

app.UseRouting();

// After routing (it needs the matched endpoint), before auth: a disabled module answers 404
// before a 401/403 could reveal that it exists.
app.UseModuleGate();

app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
