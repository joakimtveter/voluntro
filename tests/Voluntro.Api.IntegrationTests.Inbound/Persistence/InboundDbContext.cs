using Microsoft.EntityFrameworkCore;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;

namespace Voluntro.Api.IntegrationTests.Inbound.Persistence;

/// <summary>The inbound module's DbContext; schema, conventions, outbox and inbox come from <see cref="ModuleDbContext"/>.</summary>
public sealed class InboundDbContext(DbContextOptions<InboundDbContext> options) : ModuleDbContext(options), IInboundDbContext
{
    public DbSet<ReceivedOrder> ReceivedOrders => Set<ReceivedOrder>();

    protected override string Schema => InboundModule.ModuleKey;

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InboundDbContext).Assembly);
}

/// <summary>
/// For <c>dotnet ef</c>.
/// <code>dotnet ef migrations add &lt;Name&gt; --project tests/Voluntro.Api.IntegrationTests.Inbound --output-dir Persistence/Migrations</code>
/// </summary>
public sealed class InboundDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<InboundDbContext>
{
    protected override InboundDbContext Create(DbContextOptions<InboundDbContext> options) => new(options);
}
