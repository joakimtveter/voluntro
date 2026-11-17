using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Voluntro.Api.IntegrationTests.Inbound.Persistence.Configurations;

public sealed class ReceivedOrderConfiguration : IEntityTypeConfiguration<ReceivedOrder>
{
    public void Configure(EntityTypeBuilder<ReceivedOrder> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("received_orders");

        builder.Property(order => order.CustomerName).HasMaxLength(200);
    }
}
