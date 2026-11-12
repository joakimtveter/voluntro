using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Infrastructure;

namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>
/// A module that exists only in tests. It plays the part of a real module's Infrastructure project:
/// it registers its handlers, and its controllers are found because they live in its assembly.
/// </summary>
public sealed class SampleModule : IModule
{
    public string Key => "sample";

    public bool IsCore => false;

    public IReadOnlyList<string> DependsOn => [];

    public void Register(IServiceCollection services, IConfiguration config) =>
        services.AddScoped<IQueryHandler<GetSampleItem, SampleItemDto>, GetSampleItemHandler>();
}
