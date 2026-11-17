using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Voluntro.Api.IntegrationTests.Sample.Persistence.Configurations;

/// <summary>Only what the conventions can't know; see <see cref="SampleOrderConfiguration"/>.</summary>
public sealed class SampleCustomerConfiguration : IEntityTypeConfiguration<SampleCustomer>
{
    public void Configure(EntityTypeBuilder<SampleCustomer> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("customers");

        builder.Property(customer => customer.Name).HasMaxLength(200);
    }
}
