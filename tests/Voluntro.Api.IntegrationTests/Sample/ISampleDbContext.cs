using Microsoft.EntityFrameworkCore;

namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>
/// What the sample module's handlers see of its DbContext (ADR-0010): its DbSets, plus <see cref="SaveChangesAsync"/>
/// until the transaction decorator owns saving. A real module declares this in its Application project.
/// </summary>
public interface ISampleDbContext
{
    DbSet<SampleOrder> Orders { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
