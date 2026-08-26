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

        // ── Soft delete filter ──
        builder.HasQueryFilter(x => x.DeletedAt == null);
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

        // ── Soft delete filter ──
        builder.HasQueryFilter(x => x.DeletedAt == null);
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

        // Seed initial badges (BR-GAM-004) — 13 badges, 4 groups
            // Note: IconUrl uses relative path placeholders; replace with S3 URLs in production.
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            builder.HasData(
                // ── Group A: Milestone (report count) ──
                new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000001"), Code = "first_report", NameVi = "Người Khởi Đầu", NameEn = "First Reporter", Description = "Gửi báo cáo ô nhiễm đầu tiên được xác minh", IsActive = true, RequiredReportCount = (int?)1, RequiredPoints = (int?)null, RequiredStreakDays = (int?)null, RequiredActionCount = (int?)null, IconUrl = (string?)"badges/icons/first_report.png", CreatedAt = seedDate },
                new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000002"), Code = "eco_warrior", NameVi = "Chiến Binh Xanh", NameEn = "Eco Warrior", Description = "Gửi 10 báo cáo ô nhiễm được xác minh", IsActive = true, RequiredReportCount = (int?)10, RequiredPoints = (int?)null, RequiredStreakDays = (int?)null, RequiredActionCount = (int?)null, IconUrl = (string?)"badges/icons/eco_warrior.png", CreatedAt = seedDate },
                new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000003"), Code = "green_champion", NameVi = "Nhà Vô Địch Xanh", NameEn = "Green Champion", Description = "Gửi 50 báo cáo ô nhiễm được xác minh", IsActive = true, RequiredReportCount = (int?)50, RequiredPoints = (int?)null, RequiredStreakDays = (int?)null, RequiredActionCount = (int?)null, IconUrl = (string?)"badges/icons/green_champion.png", CreatedAt = seedDate },
                new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000004"), Code = "earth_guardian", NameVi = "Người Bảo Vệ Trái Đất", NameEn = "Earth Guardian", Description = "Gửi 100 báo cáo ô nhiễm được xác minh", IsActive = true, RequiredReportCount = (int?)100, RequiredPoints = (int?)null, RequiredStreakDays = (int?)null, RequiredActionCount = (int?)null, IconUrl = (string?)"badges/icons/earth_guardian.png", CreatedAt = seedDate },

                // ── Group B: Streak (consecutive days) ──
                new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000005"), Code = "streak_7d", NameVi = "Bền Bỉ 7 Ngày", NameEn = "7-Day Streak", Description = "Gửi báo cáo 7 ngày liên tiếp", IsActive = true, RequiredReportCount = (int?)null, RequiredPoints = (int?)null, RequiredStreakDays = (int?)7, RequiredActionCount = (int?)null, IconUrl = (string?)"badges/icons/streak_7d.png", CreatedAt = seedDate },
                new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000006"), Code = "streak_30d", NameVi = "Kiên Trì 30 Ngày", NameEn = "30-Day Streak", Description = "Gửi báo cáo 30 ngày liên tiếp", IsActive = true, RequiredReportCount = (int?)null, RequiredPoints = (int?)null, RequiredStreakDays = (int?)30, RequiredActionCount = (int?)null, IconUrl = (string?)"badges/icons/streak_30d.png", CreatedAt = seedDate },

                // ── Group C: Community (special actions) ──
                new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000008"), Code = "duplicate_finder", NameVi = "Người Phát Hiện Trùng", NameEn = "Duplicate Finder", Description = "5 báo cáo được xác nhận là trùng lặp, hỗ trợ phát hiện ô nhiễm", IsActive = true, RequiredReportCount = (int?)null, RequiredPoints = (int?)null, RequiredStreakDays = (int?)null, RequiredActionCount = (int?)5, IconUrl = (string?)"badges/icons/duplicate_finder.png", CreatedAt = seedDate },
                new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000009"), Code = "community_voice", NameVi = "Tiếng Nói Cộng Đồng", NameEn = "Community Voice", Description = "Có báo cáo nhận ≥ 10 lượt xác nhận từ cộng đồng", IsActive = true, RequiredReportCount = (int?)null, RequiredPoints = (int?)null, RequiredStreakDays = (int?)null, RequiredActionCount = (int?)10, IconUrl = (string?)"badges/icons/community_voice.png", CreatedAt = seedDate },
                new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000013"), Code = "cleanup_hero", NameVi = "Anh Hùng Dọn Dẹp", NameEn = "Cleanup Hero", Description = "Hoàn thành tham gia 2 chương trình dọn dẹp cộng đồng", IsActive = true, RequiredReportCount = (int?)null, RequiredPoints = (int?)null, RequiredStreakDays = (int?)null, RequiredActionCount = (int?)2, IconUrl = (string?)"badges/icons/cleanup_hero.png", CreatedAt = seedDate },

                // ── Group D: Level (point thresholds) ──
                new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000010"), Code = "rising_star", NameVi = "Ngôi Sao Đang Lên", NameEn = "Rising Star", Description = "Đạt Level 2 với 100 điểm tích lũy", IsActive = true, RequiredReportCount = (int?)null, RequiredPoints = (int?)100, RequiredStreakDays = (int?)null, RequiredActionCount = (int?)null, IconUrl = (string?)"badges/icons/rising_star.png", CreatedAt = seedDate },
                new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000011"), Code = "eco_expert", NameVi = "Chuyên Gia Môi Trường", NameEn = "Eco Expert", Description = "Đạt Level 4 với 1.500 điểm tích lũy", IsActive = true, RequiredReportCount = (int?)null, RequiredPoints = (int?)1500, RequiredStreakDays = (int?)null, RequiredActionCount = (int?)null, IconUrl = (string?)"badges/icons/eco_expert.png", CreatedAt = seedDate },
                new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000012"), Code = "green_legend", NameVi = "Huyền Thoại Xanh", NameEn = "Green Legend", Description = "Đạt Level 5 với 5.000 điểm tích lũy — thành tựu cao nhất", IsActive = true, RequiredReportCount = (int?)null, RequiredPoints = (int?)5000, RequiredStreakDays = (int?)null, RequiredActionCount = (int?)null, IconUrl = (string?)"badges/icons/green_legend.png", CreatedAt = seedDate }
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
