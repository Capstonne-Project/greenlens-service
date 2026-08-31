using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Seeders;

/// <summary>
/// Idempotent runtime seed for gamification catalog (BR-GAM-004, BR-ADM-005).
/// Complements EF HasData migrations — inserts missing badges/config rows on dev startup.
/// Note: cleanup_hero is also seeded via migration (id a1000001-…-013); seeder is fallback for older DBs.
/// </summary>
internal static class GamificationSeeder
{
    private sealed record BadgeSeed(
        string Code,
        string NameVi,
        string NameEn,
        string Description,
        int? RequiredReportCount,
        int? RequiredPoints,
        int? RequiredStreakDays = null,
        int? RequiredActionCount = null);

    private static readonly BadgeSeed[] DefaultBadges =
    [
        new("first_report", "Người Khởi Đầu", "First Reporter",
            "Gửi báo cáo ô nhiễm đầu tiên được xác minh", 1, null),
        new("eco_warrior", "Chiến Binh Xanh", "Eco Warrior",
            "Gửi 10 báo cáo ô nhiễm được xác minh", 10, null),
        new("green_champion", "Nhà Vô Địch Xanh", "Green Champion",
            "Gửi 50 báo cáo ô nhiễm được xác minh", 50, null),
        new("earth_guardian", "Người Bảo Vệ Trái Đất", "Earth Guardian",
            "Gửi 100 báo cáo ô nhiễm được xác minh", 100, null),
        new("streak_7d", "Bền Bỉ 7 Ngày", "7-Day Streak",
            "Gửi báo cáo 7 ngày liên tiếp", null, null, RequiredStreakDays: 7),
        new("streak_30d", "Kiên Trì 30 Ngày", "30-Day Streak",
            "Gửi báo cáo 30 ngày liên tiếp", null, null, RequiredStreakDays: 30),
        new("duplicate_finder", "Người Phát Hiện Trùng", "Duplicate Finder",
            "5 báo cáo được xác nhận là trùng lặp, hỗ trợ phát hiện ô nhiễm", null, null, RequiredActionCount: 5),
        new("community_voice", "Tiếng Nói Cộng Đồng", "Community Voice",
            "Có báo cáo nhận ≥ 10 lượt xác nhận từ cộng đồng", null, null, RequiredActionCount: 10),
        new("cleanup_hero", "Anh Hùng Dọn Dẹp", "Cleanup Hero",
            "Hoàn thành tham gia 2 chương trình dọn dẹp cộng đồng", null, null, RequiredActionCount: 2),
        new("rising_star", "Ngôi Sao Đang Lên", "Rising Star",
            "Đạt Level 2 với 100 điểm tích lũy", null, 100),
        new("eco_expert", "Chuyên Gia Môi Trường", "Eco Expert",
            "Đạt Level 4 với 1.500 điểm tích lũy", null, 1500),
        new("green_legend", "Huyền Thoại Xanh", "Green Legend",
            "Đạt Level 5 với 5.000 điểm tích lũy — thành tựu cao nhất", null, 5000),
    ];

    private static readonly Dictionary<string, BadgeSeed> DefaultBadgesByCode =
        DefaultBadges.ToDictionary(b => b.Code, StringComparer.OrdinalIgnoreCase);

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
        CancellationToken ct = default,
        string? r2PublicBaseUrl = null)
    {
        var publicBase = string.IsNullOrWhiteSpace(r2PublicBaseUrl)
            ? BadgeIconCatalog.DefaultPublicBase
            : r2PublicBaseUrl.Trim();

        await RemoveRetiredBadgesAsync(db, logger, ct).ConfigureAwait(false);
        await SeedBadgesAsync(db, logger, publicBase, ct).ConfigureAwait(false);
        await SyncBadgeIconUrlsAsync(db, logger, publicBase, ct).ConfigureAwait(false);
        await SeedGamificationConfigsAsync(db, logger, ct).ConfigureAwait(false);
    }

    private static async Task RemoveRetiredBadgesAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken ct)
    {
        var retired = await db.Badges
            .Where(b => BadgeIconCatalog.RetiredCodes.Contains(b.Code))
            .Select(b => b.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (retired.Count == 0)
            return;

        var userBadgeRows = await db.UserBadges
            .Where(ub => retired.Contains(ub.BadgeId))
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        var badgeRows = await db.Badges
            .Where(b => retired.Contains(b.Id))
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Removed {BadgeCount} retired badge(s) and {UserBadgeCount} user_badge row(s).",
            badgeRows, userBadgeRows);
    }

    private static async Task SeedBadgesAsync(
        ApplicationDbContext db,
        ILogger logger,
        string publicBase,
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
                BadgeIconCatalog.BuildIconUrl(seed.Code, publicBase),
                seed.RequiredPoints,
                seed.RequiredReportCount,
                seed.RequiredStreakDays,
                seed.RequiredActionCount));
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

    /// <summary>Refresh icon_url after designers replace PNGs on R2 (docs/UserBadge/icons source of truth).</summary>
    private static async Task SyncBadgeIconUrlsAsync(
        ApplicationDbContext db,
        ILogger logger,
        string publicBase,
        CancellationToken ct)
    {
        var badges = await db.Badges
            .Where(b => BadgeIconCatalog.ActiveCodes.Contains(b.Code))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var synced = 0;
        foreach (var badge in badges)
        {
            var canonical = BadgeIconCatalog.BuildIconUrl(badge.Code, publicBase);
            if (string.Equals(badge.IconUrl, canonical, StringComparison.Ordinal))
                continue;

            badge.Update(badge.NameVi, badge.NameEn, badge.Description, canonical);
            synced++;
        }

        if (synced == 0)
            return;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Synced icon_url for {Count} badge(s).", synced);
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
