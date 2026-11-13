using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Voluntro.Api.IntegrationTests.Sample.Persistence.Configurations;

public sealed class SampleOrderLineConfiguration : IEntityTypeConfiguration<SampleOrderLine>
{
    public void Configure(EntityTypeBuilder<SampleOrderLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("order_lines");

        builder.Property(line => line.ProductName).HasMaxLength(200);
    }
}
