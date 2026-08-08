using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class CommunityCleanupParticipantConfiguration : IEntityTypeConfiguration<CommunityCleanupParticipant>
{
    public void Configure(EntityTypeBuilder<CommunityCleanupParticipant> builder)
    {
        builder.ToTable("community_cleanup_participants");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.CheckInOverrideReason).HasMaxLength(500);

        // BR-CMU-005: a user can only have one participation row per event.
        builder.HasIndex(p => new { p.EventId, p.UserId }).IsUnique();
        builder.HasIndex(p => p.EventId);
        builder.HasIndex(p => p.UserId);

        builder.HasOne(p => p.Event)
            .WithMany(e => e.Participants)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
