using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.Api.IntegrationTests.Inbound.Persistence;
using Voluntro.Api.IntegrationTests.Sample.Contracts;
using Voluntro.BuildingBlocks.Infrastructure;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;

namespace Voluntro.Api.IntegrationTests.Inbound;

/// <summary>
/// A module that exists only in tests: it consumes the sample module's <see cref="OrderPlaced"/>, proving delivery
/// from one module's outbox to another module's handlers and inbox (ADR-0005). It has no controllers.
/// </summary>
public sealed class InboundModule : IModule
{
    /// <summary>The module's key, also its database schema.</summary>
    public const string ModuleKey = "inbound";

    public string Key => ModuleKey;

    public bool IsCore => false;

    public IReadOnlyList<string> DependsOn => [];

    public void Register(IServiceCollection services, IConfiguration config)
    {
        services.AddModuleDbContext<InboundDbContext>();
        services.AddScoped<IInboundDbContext>(provider => provider.GetRequiredService<InboundDbContext>());

        // Two consumers of one event, so a message can succeed for one and fail for the other.
        services.AddHandlers<InboundDbContext>()
            .AddIntegrationEventHandler<OrderPlaced, RecordReceivedOrderHandler>()
            .AddIntegrationEventHandler<OrderPlaced, RefuseFlaggedOrderHandler>();
    }
}
