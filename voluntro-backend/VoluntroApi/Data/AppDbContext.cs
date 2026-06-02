using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using VoluntroApi.Models;

namespace VoluntroApi.Data;

/// <summary>
/// The application's primary database context.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>Gets the member table.</summary>
    public DbSet<Member> Members => Set<Member>();

    /// <summary>Gets the events table.</summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>Gets the venue table.</summary>
    public DbSet<Venue> Venues => Set<Venue>();
    
    /// <summary>Gets the group table.</summary>
    public DbSet<Group> Groups => Set<Group>();
    
    /// <summary>Gets the member-group membership join table.</summary>
    public DbSet<MemberGroup> MemberGroups => Set<MemberGroup>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Member>().Property(m => m.Id).HasValueGenerator<GuidV7Generator>();
        modelBuilder.Entity<Event>().Property(e => e.Id).HasValueGenerator<GuidV7Generator>();
        modelBuilder.Entity<Venue>().Property(v => v.Id).HasValueGenerator<GuidV7Generator>();
        modelBuilder.Entity<Group>().Property(g => g.Id).HasValueGenerator<GuidV7Generator>();
        
        modelBuilder.Entity<MemberGroup>(entity =>
        {
            entity.HasKey(mg => new { mg.MemberId, mg.GroupId });

            entity.HasOne(mg => mg.Member)
                .WithMany()
                .HasForeignKey(mg => mg.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(mg => mg.Group)
                .WithMany(g => g.MemberGroups)
                .HasForeignKey(mg => mg.GroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        modelBuilder.Entity<Group>()
            .HasOne(g => g.ParentGroup)
            .WithMany(g => g.ChildGroups)
            .HasForeignKey(g => g.ParentGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
    
}

/// <summary>
/// Generates time-ordered, RFC 4122 compliant UUID v7 values for entity primary keys.
/// </summary>
file sealed class GuidV7Generator : ValueGenerator<Guid>
{
    /// <inheritdoc/>
    public override Guid Next(EntityEntry entry) => Guid.CreateVersion7();

    /// <inheritdoc/>
    public override bool GeneratesTemporaryValues => false;
}
