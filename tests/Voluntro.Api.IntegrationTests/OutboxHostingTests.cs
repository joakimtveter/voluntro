using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Voluntro.Api.IntegrationTests.Inbound;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.BuildingBlocks.Infrastructure;
using Voluntro.BuildingBlocks.Infrastructure.Outbox;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// Who runs the outbox in the background (ADR-0014): the API does, through <c>AddOutboxProcessing</c>; registering
/// modules never starts anything, which is what keeps the MigrationService, which registers the same modules, from
/// delivering or deleting messages. No database needed.
/// </summary>
public sealed class OutboxHostingTests
{
    // Program.cs itself, not VoluntroApiFactory, which takes both loops out so tests can drive them.
    [Fact]
    public void The_api_runs_outbox_delivery_and_cleanup_in_the_background()
    {
        using var api = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Production);
            builder.UseSetting("App:PublicBaseUrl", "https://voluntro.test");
            builder.UseSetting("ConnectionStrings:voluntro", "Host=localhost;Database=never_connected");
        });

        var hosted = api.Services.GetServices<IHostedService>().Select(service => service.GetType()).ToList();

        Assert.Contains(typeof(OutboxProcessingService), hosted);
        Assert.Contains(typeof(OutboxCleanupService), hosted);
    }

    // The MigrationService registers modules with AddModules and nothing more.
    [Fact]
    public void Registering_modules_adds_no_hosted_service()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection()
            .AddModule(new SampleModule(), config)
            .AddModule(new InboundModule(), config);

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IHostedService));

        // The processors are registered, ready for the API's loop, but nothing would run them.
        Assert.Equal(2, services.Count(descriptor => descriptor.ServiceType == typeof(IOutboxProcessor)));
    }
}
