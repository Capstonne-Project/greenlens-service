using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Gamification;

internal static class GamificationHelpers
{
    internal const int MinLeaderboardYear = 2026;

    internal static int CalculateLevel(int totalPoints) => totalPoints switch
    {
        >= 5000 => 5,
        >= 1500 => 4,
        >= 500 => 3,
        >= 100 => 2,
        _ => 1
    };

    internal static (DateTime Start, DateTime End, int? Year, int? Month) GetPeriodRange(
        LeaderboardPeriod period,
        int? year = null,
        int? month = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        return period switch
        {
            LeaderboardPeriod.Weekly => (
                now.AddDays(-(int)now.DayOfWeek).Date,
                now.AddDays(7 - (int)now.DayOfWeek).Date,
                null,
                null),
            LeaderboardPeriod.Monthly => GetMonthlyRange(
                year ?? now.Year,
                month ?? now.Month),
            LeaderboardPeriod.Yearly => GetYearlyRange(year ?? now.Year),
            LeaderboardPeriod.AllTime => throw new ArgumentOutOfRangeException(
                nameof(period),
                "AllTime does not have a bounded period range."),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
    }

    private static (DateTime Start, DateTime End, int? Year, int? Month) GetMonthlyRange(
        int year,
        int month)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddMonths(1), year, month);
    }

    private static (DateTime Start, DateTime End, int? Year, int? Month) GetYearlyRange(int year)
    {
        var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddYears(1), year, null);
    }
}
