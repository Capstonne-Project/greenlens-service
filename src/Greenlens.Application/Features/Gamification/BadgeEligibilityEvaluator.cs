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
            "cleanup_hero" => metrics.CompletedCleanupCount >= 1,
            // TODO: enable when BR-MAP-010 hotspot detection is implemented
            "hotspot_hunter" => false,
            _ => badge.RequiredPoints.HasValue && totalPoints >= badge.RequiredPoints.Value
                || badge.RequiredReportCount.HasValue
                    && metrics.VerifiedReportCount >= badge.RequiredReportCount.Value
        };
    }

    /// <summary>
    /// User's current value on the badge's progress axis (points, report count, streak days, …).
    /// Null when the badge has no numeric progress axis (boolean conditions like community_voice,
    /// or not-yet-implemented conditions like hotspot_hunter).
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
            "rising_star" or "eco_expert" or "green_legend" => totalPoints,
            "community_voice" => null,
            "hotspot_hunter" => null,
            _ => badge.RequiredPoints.HasValue
                ? totalPoints
                : badge.RequiredReportCount.HasValue
                    ? metrics.VerifiedReportCount
                    : null
        };
    }
}
