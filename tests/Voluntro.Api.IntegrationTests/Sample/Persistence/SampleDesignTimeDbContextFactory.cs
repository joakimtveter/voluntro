using Microsoft.EntityFrameworkCore;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;

namespace Voluntro.Api.IntegrationTests.Sample.Persistence;

/// <summary>
/// For <c>dotnet ef</c>. Without it, the tool would try to start this project's entry point, which is the test runner.
/// <code>dotnet ef migrations add &lt;Name&gt; --project tests/Voluntro.Api.IntegrationTests --output-dir Sample/Persistence/Migrations</code>
/// </summary>
public sealed class SampleDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<SampleDbContext>
{
    protected override SampleDbContext Create(DbContextOptions<SampleDbContext> options) => new(options);
}
