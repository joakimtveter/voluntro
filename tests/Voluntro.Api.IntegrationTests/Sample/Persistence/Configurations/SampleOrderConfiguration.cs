using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Voluntro.Api.IntegrationTests.Sample.Persistence.Configurations;

/// <summary>
/// Only what the conventions can't know. Keys, ID conversions, snake_case names, the enum as text and
/// Version on xmin come from <c>ModuleDbContext</c>.
/// </summary>
public sealed class SampleOrderConfiguration : IEntityTypeConfiguration<SampleOrder>
{
    public void Configure(EntityTypeBuilder<SampleOrder> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("orders");

        builder.Property(order => order.CustomerName).HasMaxLength(200);

        // Lines is a read-only view; EF reads and writes the private _lines field behind it.
        builder.HasMany(order => order.Lines)
            .WithOne()
            .HasForeignKey(line => line.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
