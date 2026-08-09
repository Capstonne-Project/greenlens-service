using Greenlens.Domain.Entities;

namespace Greenlens.Application.Features.Gamification;

/// <summary>Evaluates badge eligibility from precomputed metrics (BR-GAM-004).</summary>
internal static class BadgeEligibilityEvaluator
{
    internal static bool IsEligible(Badge badge, int totalPoints, BadgeEligibilityMetrics metrics)
    {
        return badge.Code switch
        {
            "first_report" => metrics.VerifiedReportCount >= 1,
            "eco_warrior" => metrics.VerifiedReportCount >= 10,
            "streak_7d" => metrics.MaxSubmitStreakDays >= 7,
            "streak_30d" => metrics.MaxSubmitStreakDays >= 30,
            "duplicate_finder" => metrics.DuplicateReportCount >= 5,
            "community_voice" => metrics.HasCommunityVoice,
            "cleanup_hero" => metrics.CompletedCleanupCount >= 2,
            "hotspot_hunter" => metrics.HotspotReportCount >= 3,
            _ => badge.RequiredPoints.HasValue && totalPoints >= badge.RequiredPoints.Value
                || badge.RequiredReportCount.HasValue
                    && metrics.VerifiedReportCount >= badge.RequiredReportCount.Value
        };
    }

    /// <summary>
    /// User's current value on the badge's progress axis (points, report count, streak days, …).
    /// Null only when the badge has no numeric progress axis.
    /// </summary>
    internal static int? GetCurrentProgressValue(Badge badge, int totalPoints, BadgeEligibilityMetrics metrics)
    {
        return badge.Code switch
        {
            "first_report" or "eco_warrior" or "green_champion" or "earth_guardian"
                => metrics.VerifiedReportCount,
            "streak_7d" or "streak_30d" => metrics.MaxSubmitStreakDays,
            "duplicate_finder" => metrics.DuplicateReportCount,
            "cleanup_hero" => metrics.CompletedCleanupCount,
            "community_voice" => metrics.MaxReporterCount,
            "hotspot_hunter" => metrics.HotspotReportCount,
            "rising_star" or "eco_expert" or "green_legend" => totalPoints,
            _ => badge.RequiredPoints.HasValue
                ? totalPoints
                : badge.RequiredReportCount.HasValue
                    ? metrics.VerifiedReportCount
                    : badge.RequiredStreakDays.HasValue
                        ? metrics.MaxSubmitStreakDays
                        : null
        };
    }

    /// <summary>
    /// The numeric target the badge's progress axis must reach to be earned.
    /// </summary>
    internal static int? GetTargetValue(Badge badge)
    {
        return badge.Code switch
        {
            "first_report" => 1,
            "eco_warrior" => 10,
            "green_champion" => 50,
            "earth_guardian" => 100,
            "cleanup_hero" => 2,
            "streak_7d" => 7,
            "streak_30d" => 30,
            "duplicate_finder" => 5,
            "community_voice" => 10,
            "hotspot_hunter" => 3,
            "rising_star" => 100,
            "eco_expert" => 1500,
            "green_legend" => 5000,
            _ => badge.RequiredPoints ?? badge.RequiredReportCount ?? badge.RequiredStreakDays
        };
    }

    /// <summary>
    /// Hint for clients on how to label progress (e.g. verified_reports → "báo cáo").
    /// </summary>
    internal static string? GetProgressMetric(Badge badge) =>
        badge.Code switch
        {
            "first_report" or "eco_warrior" or "green_champion" or "earth_guardian"
                => "verified_reports",
            "streak_7d" or "streak_30d" => "streak_days",
            "duplicate_finder" => "duplicate_reports",
            "cleanup_hero" => "cleanup_events",
            "community_voice" => "reporter_count",
            "hotspot_hunter" => "hotspot_reports",
            "rising_star" or "eco_expert" or "green_legend" => "points",
            _ => badge.RequiredPoints.HasValue ? "points"
                : badge.RequiredReportCount.HasValue ? "verified_reports"
                : badge.RequiredStreakDays.HasValue ? "streak_days"
                : null
        };
}
