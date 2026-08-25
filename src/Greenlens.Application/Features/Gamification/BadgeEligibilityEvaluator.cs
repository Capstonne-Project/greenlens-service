using Greenlens.Domain.Entities;

namespace Greenlens.Application.Features.Gamification;

/// <summary>Evaluates badge eligibility from precomputed metrics (BR-GAM-004).</summary>
internal static class BadgeEligibilityEvaluator
{
    internal static bool IsEligible(Badge badge, int totalPoints, BadgeEligibilityMetrics metrics)
    {
        var current = GetCurrentProgressValue(badge, totalPoints, metrics);
        var target = GetTargetValue(badge);

        return current.HasValue && target.HasValue && current.Value >= target.Value;
    }

    /// <summary>
    /// User's current value on the badge's progress axis (points, report count, streak days, …).
    /// Null only when the badge has no numeric progress axis.
    /// </summary>
    internal static int? GetCurrentProgressValue(Badge badge, int totalPoints, BadgeEligibilityMetrics metrics) =>
        BadgeThresholdPolicy.GetAxis(badge.Code) switch
        {
            BadgeThresholdAxis.Points => totalPoints,
            BadgeThresholdAxis.StreakDays => metrics.MaxSubmitStreakDays,
            BadgeThresholdAxis.ActionCount => badge.Code switch
            {
                "duplicate_finder" => metrics.DuplicateReportCount,
                "community_voice" => metrics.MaxReporterCount,
                "cleanup_hero" => metrics.CompletedCleanupCount,
                _ => null
            },
            _ => metrics.VerifiedReportCount
        };

    /// <summary>The numeric target the badge's progress axis must reach to be earned.</summary>
    internal static int? GetTargetValue(Badge badge) => BadgeThresholdPolicy.GetThreshold(badge);

    /// <summary>
    /// True when the user is close to earning a not-yet-unlocked badge:
    /// at least halfway, one step away, or 0/1 for single-step badges (BR-GAM-004, BR-NTF-002).
    /// </summary>
    internal static bool IsNearProgress(int current, int target)
    {
        if (target <= 1 || current >= target)
            return false;

        var halfwayThreshold = (target + 1) / 2;
        return current >= target - 1 || current >= halfwayThreshold;
    }

    /// <summary>Hint for clients on how to label progress (e.g. verified_reports → "báo cáo").</summary>
    internal static string? GetProgressMetric(Badge badge) =>
        BadgeThresholdPolicy.GetProgressMetric(badge.Code);
}
