using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Voluntro.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// The <c>outbox</c> table, in every module's schema. Column names come from the snake_case convention.
/// </summary>
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox");

        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.Payload).HasColumnType("jsonb");

        // What the processor reads: the few messages not yet processed or failed, by when they are due.
        builder.HasIndex(message => message.NextAttemptAt)
            .HasFilter("processed_at IS NULL AND failed_at IS NULL");
    }
}

/// <summary>The <c>inbox</c> table, in every module's schema: one row per message and consumer.</summary>
internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox");

        builder.HasKey(message => new { message.MessageId, message.Consumer });
    }
}
