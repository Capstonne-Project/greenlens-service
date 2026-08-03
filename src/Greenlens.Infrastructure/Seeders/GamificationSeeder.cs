using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Seeders;

/// <summary>
/// Idempotent runtime seed for gamification catalog (BR-GAM-004, BR-ADM-005).
/// Complements EF HasData migrations — inserts missing badges/config rows on dev startup.
/// </summary>
internal static class GamificationSeeder
{
    private sealed record BadgeSeed(
        string Code,
        string NameVi,
        string NameEn,
        string Description,
        string IconUrl,
        int? RequiredReportCount,
        int? RequiredPoints,
        int? RequiredStreakDays = null);

    private static readonly BadgeSeed[] DefaultBadges =
    [
        new("first_report", "Người Khởi Đầu", "First Reporter",
            "Gửi báo cáo ô nhiễm đầu tiên được xác minh", "badges/icons/first_report.png", 1, null),
        new("eco_warrior", "Chiến Binh Xanh", "Eco Warrior",
            "Gửi 10 báo cáo ô nhiễm được xác minh", "badges/icons/eco_warrior.png", 10, null),
        new("green_champion", "Nhà Vô Địch Xanh", "Green Champion",
            "Gửi 50 báo cáo ô nhiễm được xác minh", "badges/icons/green_champion.png", 50, null),
        new("earth_guardian", "Người Bảo Vệ Trái Đất", "Earth Guardian",
            "Gửi 100 báo cáo ô nhiễm được xác minh", "badges/icons/earth_guardian.png", 100, null),
        new("streak_7d", "Bền Bỉ 7 Ngày", "7-Day Streak",
            "Gửi báo cáo 7 ngày liên tiếp", "badges/icons/streak_7d.png", null, null, RequiredStreakDays: 7),
        new("streak_30d", "Kiên Trì 30 Ngày", "30-Day Streak",
            "Gửi báo cáo 30 ngày liên tiếp", "badges/icons/streak_30d.png", null, null, RequiredStreakDays: 30),
        new("hotspot_hunter", "Thợ Săn Điểm Nóng", "Hotspot Hunter",
            "Gửi 3 báo cáo trong khu vực hotspot ô nhiễm", "badges/icons/hotspot_hunter.png", null, null),
        new("duplicate_finder", "Người Phát Hiện Trùng", "Duplicate Finder",
            "5 báo cáo được xác nhận là trùng lặp, hỗ trợ phát hiện ô nhiễm", "badges/icons/duplicate_finder.png", null, null),
        new("community_voice", "Tiếng Nói Cộng Đồng", "Community Voice",
            "Có báo cáo nhận ≥ 10 lượt xác nhận từ cộng đồng", "badges/icons/community_voice.png", null, null),
        new("rising_star", "Ngôi Sao Đang Lên", "Rising Star",
            "Đạt Level 2 với 100 điểm tích lũy", "badges/icons/rising_star.png", null, 100),
        new("eco_expert", "Chuyên Gia Môi Trường", "Eco Expert",
            "Đạt Level 4 với 1.500 điểm tích lũy", "badges/icons/eco_expert.png", null, 1500),
        new("green_legend", "Huyền Thoại Xanh", "Green Legend",
            "Đạt Level 5 với 5.000 điểm tích lũy — thành tựu cao nhất", "badges/icons/green_legend.png", null, 5000),
        new("cleanup_hero", "Anh Hùng Dọn Dẹp", "Cleanup Hero",
            "Hoàn thành tham gia một chương trình dọn dẹp cộng đồng", "badges/icons/cleanup_hero.png", null, null),
    ];

    private static readonly (PointReason Action, int Points, string Description)[] DefaultConfigs =
    [
        (PointReason.ReportVerified, 10, "Báo cáo được xác minh bởi LEO"),
        (PointReason.ReportResolved, 20, "Báo cáo đã xử lý xong (cleanup hoàn tất)"),
        (PointReason.PenaltyIssued, 20, "Biên bản xử phạt được ban hành"),
        (PointReason.DuplicateReport, 5, "Báo cáo trùng được gộp: +50% điểm báo cáo gốc (ReportVerified)"),
        (PointReason.ReportRejected, -5, "Báo cáo bị từ chối (không hợp lệ)"),
        (PointReason.FraudPenalty, -100, "BR-GAM-006: Phạt gian lận — trừ toàn bộ điểm"),
        (PointReason.CommunityCleanupParticipation, 15, "Tham gia và check-in một chương trình dọn dẹp cộng đồng đã hoàn thành"),
    ];

    public static async Task SeedAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken ct = default)
    {
        await SeedBadgesAsync(db, logger, ct).ConfigureAwait(false);
        await SeedGamificationConfigsAsync(db, logger, ct).ConfigureAwait(false);
    }

    private static async Task SeedBadgesAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken ct)
    {
        var existingCodes = await db.Badges
            .AsNoTracking()
            .Select(b => b.Code)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var existingSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = 0;

        foreach (var seed in DefaultBadges)
        {
            if (existingSet.Contains(seed.Code))
                continue;

            db.Badges.Add(Badge.Create(
                seed.Code,
                seed.NameVi,
                seed.NameEn,
                seed.Description,
                seed.IconUrl,
                seed.RequiredPoints,
                seed.RequiredReportCount,
                seed.RequiredStreakDays));
            added++;
        }

        if (added == 0)
        {
            logger.LogDebug("Gamification badges up to date — nothing to seed.");
            return;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Gamification badges seeded: {Count} added.", added);
    }

    private static async Task SeedGamificationConfigsAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken ct)
    {
        var existingActions = await db.GamificationConfigs
            .AsNoTracking()
            .Select(c => c.ActionType)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var existingSet = existingActions.ToHashSet();
        var added = 0;

        foreach (var (action, points, description) in DefaultConfigs)
        {
            if (existingSet.Contains(action))
                continue;

            db.GamificationConfigs.Add(GamificationConfig.Create(action, points, description));
            added++;
        }

        if (added == 0)
        {
            logger.LogDebug("Gamification configs up to date — nothing to seed.");
            return;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Gamification configs seeded: {Count} added.", added);
    }
}
