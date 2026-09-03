using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Gamification;

/// <summary>Redis key builder cho BR-GAM-005 — pattern <c>gamification:leaderboard:*</c>.</summary>
public static class LeaderboardCacheKeys
{
    private const string Prefix = "gamification:leaderboard";

    public static string Build(
        LeaderboardPeriod period,
        int top,
        int? year = null,
        int? month = null,
        DateTime? utcNow = null)
    {
        return period switch
        {
            LeaderboardPeriod.AllTime => $"{Prefix}:all-time:top:{top}",
            LeaderboardPeriod.Weekly => BuildBoundedKey("weekly", top, period, year, month, utcNow),
            LeaderboardPeriod.Monthly => BuildBoundedKey("monthly", top, period, year, month, utcNow),
            LeaderboardPeriod.Yearly => BuildBoundedKey("yearly", top, period, year, month, utcNow),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unsupported leaderboard period.")
        };
    }

    private static string BuildBoundedKey(
        string segment,
        int top,
        LeaderboardPeriod period,
        int? year,
        int? month,
        DateTime? utcNow)
    {
        var (start, _, resolvedYear, resolvedMonth) = GamificationHelpers.GetPeriodRange(
            period,
            year,
            month,
            utcNow);

        return period switch
        {
            LeaderboardPeriod.Weekly =>
                $"{Prefix}:{segment}:{start:yyyyMMdd}:top:{top}",
            LeaderboardPeriod.Monthly =>
                $"{Prefix}:{segment}:{resolvedYear}:{resolvedMonth}:top:{top}",
            LeaderboardPeriod.Yearly =>
                $"{Prefix}:{segment}:{resolvedYear}:top:{top}",
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Expected bounded period.")
        };
    }
}
