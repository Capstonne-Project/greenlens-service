using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class GamificationConfigConfiguration : IEntityTypeConfiguration<GamificationConfig>
{
    public void Configure(EntityTypeBuilder<GamificationConfig> builder)
    {
        builder.ToTable("gamification_configs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ActionType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        // One config per action type
        builder.HasIndex(x => x.ActionType)
            .IsUnique()
            .HasDatabaseName("ix_gamification_config_action_type");

        // Seed default point configurations (BR-ADM-005, BR-GAM-001)
        var now = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            Seed(new Guid("a0000001-0000-0000-0000-000000000001"), PointReason.ReportVerified, 10,
                "Báo cáo được xác minh bởi LEO", now),
            Seed(new Guid("a0000001-0000-0000-0000-000000000002"), PointReason.ReportResolved, 20,
                "Báo cáo đã xử lý xong (cleanup hoàn tất)", now),
            Seed(new Guid("a0000001-0000-0000-0000-000000000003"), PointReason.PenaltyIssued, 20,
                "Biên bản xử phạt được ban hành", now),
            Seed(new Guid("a0000001-0000-0000-0000-000000000004"), PointReason.DuplicateReport, 5,
                "Báo cáo trùng được gộp: +50% điểm báo cáo gốc (ReportVerified). Giá trị Points chỉ mang tính tham chiếu; runtime tính động.", now),
            Seed(new Guid("a0000001-0000-0000-0000-000000000006"), PointReason.FraudPenalty, -100,
                "Phạt gian lận — trừ toàn bộ điểm", now),
            Seed(new Guid("a0000001-0000-0000-0000-000000000007"), PointReason.CommunityCleanupParticipation, 15,
                "Tham gia và check-in một chương trình dọn dẹp cộng đồng đã hoàn thành", now));
    }

    /// <summary>
    /// Creates an anonymous object for HasData seeding (EF Core requires this pattern
    /// because it bypasses constructors and sets properties directly).
    /// </summary>
    private static object Seed(Guid id, PointReason action, int points, string desc, DateTime now)
        => new
        {
            Id = id,
            ActionType = action,
            Points = points,
            IsActive = true,
            Description = desc,
            CreatedAt = now,
            UpdatedAt = (DateTime?)null
        };
}
