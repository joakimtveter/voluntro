using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.Api.IntegrationTests.Sample.Persistence;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Contracts;
using Voluntro.BuildingBlocks.Infrastructure;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;

namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>
/// A module that exists only in tests. It plays the part of a real module's Infrastructure project:
/// it registers its handlers and DbContext, and its controllers are found because they live in its assembly.
/// </summary>
public sealed class SampleModule : IModule
{
    /// <summary>The module's key, also its database schema.</summary>
    public const string ModuleKey = "sample";

    public string Key => ModuleKey;

    public bool IsCore => false;

    public IReadOnlyList<string> DependsOn => [];

    public void Register(IServiceCollection services, IConfiguration config)
    {
        services.AddModuleDbContext<SampleDbContext>();
        services.AddScoped<ISampleDbContext>(provider => provider.GetRequiredService<SampleDbContext>());

        services.AddScoped<IQueryHandler<GetSampleItem, SampleItemDto>, GetSampleItemHandler>();
        services.AddScoped<IQueryHandler<ListSampleItems, PagedList<SampleItemDto>>, ListSampleItemsHandler>();

        services.AddScoped<IQueryHandler<GetSampleOrder, SampleOrderDto>, GetSampleOrderHandler>();

        services.AddCommandHandlers<SampleDbContext>()
            .AddValidated<CreateSampleOrder, SampleOrderId, CreateSampleOrderHandler, CreateSampleOrderValidator>()
            .AddValidated<UpdateSampleOrder, UpdateSampleOrderHandler, UpdateSampleOrderValidator>()
            .AddValidated<AddSampleOrderLine, SampleOrderLineId, AddSampleOrderLineHandler, AddSampleOrderLineValidator>()
            .Add<PlaceSampleOrder, PlaceSampleOrderHandler>();
    }
}
