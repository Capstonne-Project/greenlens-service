using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Gamification;

/// <summary>
/// Loads a user's total points and badge-eligibility metrics — shared by CheckBadgesCommandHandler
/// (award decisions) and GetBadgeCatalogQueryHandler (progress display).
/// </summary>
internal static class BadgeMetricsProvider
{
    public static async Task<(int TotalPoints, BadgeEligibilityMetrics Metrics)> LoadAsync(
        Guid userId,
        IUserPointsRepository userPointsRepo,
        IReportRepository reportRepo,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var userPoints = await userPointsRepo.GetByUserIdAsync(userId, ct).ConfigureAwait(false);
        var totalPoints = userPoints?.TotalPoints ?? 0;

        var userReports = reportRepo.QueryAsNoTracking()
            .Where(r => r.ReporterId == userId);

        var verifiedReportCount = await userReports
            .CountAsync(r => VerifiedReportStatusFilter.CountedStatuses.Contains(r.Status), ct)
            .ConfigureAwait(false);

        var duplicateReportCount = await userReports
            .CountAsync(r => r.Status == ReportStatus.Duplicate, ct)
            .ConfigureAwait(false);

        var hasCommunityVoice = await userReports
            .AnyAsync(r => r.ReporterCount >= 10, ct)
            .ConfigureAwait(false);

        var maxReporterCount = await userReports
            .MaxAsync(r => (int?)r.ReporterCount, ct)
            .ConfigureAwait(false) ?? 0;

        var submitTimestamps = await userReports
            .Select(r => r.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var maxSubmitStreakDays = ReportStreakCalculator.ComputeMaxConsecutiveDays(submitTimestamps);

        var completedCleanupCount = await db.Set<CommunityCleanupParticipant>()
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.Status == CommunityCleanupParticipantStatus.CheckedIn)
            .Join(db.Set<CommunityCleanupEvent>().Where(e => e.Status == CommunityCleanupStatus.Completed),
                p => p.EventId, e => e.Id, (p, e) => p.Id)
            .CountAsync(ct)
            .ConfigureAwait(false);

        var metrics = new BadgeEligibilityMetrics(
            verifiedReportCount,
            duplicateReportCount,
            hasCommunityVoice,
            maxReporterCount,
            maxSubmitStreakDays,
            completedCleanupCount,
            HotspotReportCount: 0); // TODO: BR-MAP-010 hotspot detection

        return (totalPoints, metrics);
    }
}
