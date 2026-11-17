using Microsoft.EntityFrameworkCore;

namespace Voluntro.Api.IntegrationTests.Inbound;

/// <summary>What the inbound module's handlers see of its DbContext (ADR-0010).</summary>
public interface IInboundDbContext
{
    DbSet<ReceivedOrder> ReceivedOrders { get; }
}
