using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class CommunityCleanupEventConfiguration : IEntityTypeConfiguration<CommunityCleanupEvent>
{
    public void Configure(EntityTypeBuilder<CommunityCleanupEvent> builder)
    {
        builder.ToTable("community_cleanup_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.MeetingNote).HasMaxLength(500);
        builder.Property(e => e.ProgressNote).HasMaxLength(1000);
        builder.Property(e => e.RejectionReason).HasMaxLength(500);
        builder.Property(e => e.CancelReason).HasMaxLength(500);
        builder.Property(e => e.FacebookPostId).HasMaxLength(100);
        builder.Property(e => e.FacebookPageUrl).HasMaxLength(500);
        builder.Property(e => e.MaxParticipants).HasDefaultValue(50);
        builder.Property(e => e.ProgressPercent).HasDefaultValue(0);

        builder.HasIndex(e => e.ReportId).HasDatabaseName("ix_community_cleanup_events_report_id");
        builder.HasIndex(e => new { e.Status, e.StartsAt });
        builder.HasIndex(e => new { e.LeaderUserId, e.Status });

        // BR-CMU-003: one active event per report.
        builder.HasIndex(e => e.ReportId)
            .HasDatabaseName("ix_community_cleanup_events_active_per_report")
            .IsUnique()
            .HasFilter($"status NOT IN ('{CommunityCleanupStatus.Completed}', '{CommunityCleanupStatus.Cancelled}') AND deleted_at IS NULL");

        builder.HasOne(e => e.Report)
            .WithMany()
            .HasForeignKey(e => e.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CreatedByLeo)
            .WithMany()
            .HasForeignKey(e => e.CreatedByLeoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.LeaderUser)
            .WithMany()
            .HasForeignKey(e => e.LeaderUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.LeaderTeam)
            .WithMany()
            .HasForeignKey(e => e.LeaderTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Participants)
            .WithOne(p => p.Event)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
