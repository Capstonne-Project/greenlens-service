using Greenlens.Domain.Entities;

namespace Greenlens.Application.Features.Gamification;

internal enum BadgeThresholdAxis
{
    VerifiedReports,
    StreakDays,
    Points,
    ActionCount
}

/// <summary>Maps badge codes to progress axis and persisted threshold fields (BR-GAM-004, BR-ADM-005).</summary>
internal static class BadgeThresholdPolicy
{
    internal static BadgeThresholdAxis GetAxis(string code) =>
        code switch
        {
            "rising_star" or "eco_expert" or "green_legend" => BadgeThresholdAxis.Points,
            "streak_7d" or "streak_30d" => BadgeThresholdAxis.StreakDays,
            "duplicate_finder" or "community_voice" or "cleanup_hero" => BadgeThresholdAxis.ActionCount,
            _ => BadgeThresholdAxis.VerifiedReports
        };

    internal static int? GetThreshold(Badge badge) =>
        GetAxis(badge.Code) switch
        {
            BadgeThresholdAxis.Points => badge.RequiredPoints,
            BadgeThresholdAxis.StreakDays => badge.RequiredStreakDays,
            BadgeThresholdAxis.ActionCount => badge.RequiredActionCount,
            _ => badge.RequiredReportCount
        };

    internal static string GetProgressMetric(string code) =>
        code switch
        {
            "streak_7d" or "streak_30d" => "streak_days",
            "duplicate_finder" => "duplicate_reports",
            "cleanup_hero" => "cleanup_events",
            "community_voice" => "reporter_count",
            "rising_star" or "eco_expert" or "green_legend" => "points",
            _ => "verified_reports"
        };
}
