using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class UserPointsConfiguration : IEntityTypeConfiguration<UserPoints>
{
    public void Configure(EntityTypeBuilder<UserPoints> builder)
    {
        builder.ToTable("user_points");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId).IsUnique();

        builder.Property(x => x.TotalPoints).HasDefaultValue(0);
        builder.Property(x => x.IsLocked).HasDefaultValue(false);
        builder.Property(x => x.LockedReason).HasMaxLength(500);

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<UserPoints>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Transactions)
            .WithOne(x => x.UserPointsAggregate)
            .HasForeignKey(x => x.UserPointsId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite index for leaderboard queries
        builder.HasIndex(x => new { x.IsLocked, x.TotalPoints })
            .HasDatabaseName("ix_user_points_leaderboard");
    }
}

internal sealed class PointTransactionConfiguration : IEntityTypeConfiguration<PointTransaction>
{
    public void Configure(EntityTypeBuilder<PointTransaction> builder)
    {
        builder.ToTable("point_transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).HasConversion<string>().HasMaxLength(50);

        // Idempotency index: one transaction per report + reason
        builder.HasIndex(x => new { x.UserPointsId, x.ReportId, x.Reason })
            .HasDatabaseName("ix_point_tx_idempotent")
            .HasFilter("\"report_id\" IS NOT NULL")
            .IsUnique();

        // Period query index (leaderboard)
        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("ix_point_tx_created");
    }
}

internal sealed class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.ToTable("badges");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.NameVi).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.IconUrl).HasMaxLength(500);

        // Seed initial badges (BR-GAM-004)
        builder.HasData(
            new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000001"), Code = "first_report", NameVi = "Người khởi đầu", NameEn = "First Report", Description = "Gửi báo cáo ô nhiễm đầu tiên", IsActive = true, RequiredReportCount = (int?)1, RequiredPoints = (int?)null, IconUrl = (string?)null, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000002"), Code = "eco_warrior", NameVi = "Chiến binh Xanh", NameEn = "Eco Warrior", Description = "Gửi 10 báo cáo ô nhiễm được xác minh", IsActive = true, RequiredReportCount = (int?)10, RequiredPoints = (int?)null, IconUrl = (string?)null, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000003"), Code = "hotspot_hunter", NameVi = "Thợ săn điểm nóng", NameEn = "Hotspot Hunter", Description = "Gửi 3 báo cáo trong vùng hotspot", IsActive = true, RequiredReportCount = (int?)null, RequiredPoints = (int?)null, IconUrl = (string?)null, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000004"), Code = "streak_7d", NameVi = "7 ngày liên tiếp", NameEn = "7-Day Streak", Description = "Gửi báo cáo 7 ngày liên tiếp", IsActive = true, RequiredReportCount = (int?)null, RequiredPoints = (int?)null, IconUrl = (string?)null, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}

internal sealed class UserBadgeConfiguration : IEntityTypeConfiguration<UserBadge>
{
    public void Configure(EntityTypeBuilder<UserBadge> builder)
    {
        builder.ToTable("user_badges");

        builder.HasKey(x => x.Id);

        // One badge per user (composite unique)
        builder.HasIndex(x => new { x.UserId, x.BadgeId })
            .HasDatabaseName("ix_user_badge_unique")
            .IsUnique();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Badge)
            .WithMany()
            .HasForeignKey(x => x.BadgeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
