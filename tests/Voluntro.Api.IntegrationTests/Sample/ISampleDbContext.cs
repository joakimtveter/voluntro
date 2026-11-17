using Microsoft.EntityFrameworkCore;

namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>
/// What the sample module's handlers see of its DbContext (ADR-0010): its DbSets only. Saving belongs to the
/// transaction decorator (ADR-0012). A real module declares this in its Application project.
/// </summary>
public interface ISampleDbContext
{
    DbSet<SampleOrder> Orders { get; }
}
