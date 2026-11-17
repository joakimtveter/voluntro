using Microsoft.EntityFrameworkCore;
using Voluntro.BuildingBlocks.Application;

namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>
/// What the sample module's handlers see of its DbContext (ADR-0010): its DbSets, and the module's outbox to publish
/// integration events. Saving belongs to the transaction decorator (ADR-0012). A real module declares this in its
/// Application project.
/// </summary>
public interface ISampleDbContext : IOutbox
{
    DbSet<SampleOrder> Orders { get; }

    DbSet<SampleCustomer> Customers { get; }
}
