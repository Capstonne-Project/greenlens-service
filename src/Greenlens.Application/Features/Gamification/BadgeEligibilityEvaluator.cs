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
            // TODO: enable when BR-MAP-010 hotspot detection is implemented
            "hotspot_hunter" => false,
            _ => badge.RequiredPoints.HasValue && totalPoints >= badge.RequiredPoints.Value
                || badge.RequiredReportCount.HasValue
                    && metrics.VerifiedReportCount >= badge.RequiredReportCount.Value
        };
    }
}
