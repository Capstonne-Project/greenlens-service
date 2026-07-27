namespace Greenlens.Application.Features.Gamification;

/// <summary>
/// Computes longest consecutive submit-day streak using Vietnam calendar dates (BR-GAM-004).
/// Application-layer only — does not change PostgreSQL timezone settings.
/// </summary>
internal static class ReportStreakCalculator
{
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    /// <summary>
    /// Returns the longest run of consecutive calendar days (Asia/Ho_Chi_Minh) with at least one submit.
    /// </summary>
    internal static int ComputeMaxConsecutiveDays(IReadOnlyList<DateTime> submitTimestampsUtc)
    {
        if (submitTimestampsUtc.Count == 0)
            return 0;

        var distinctVnDates = submitTimestampsUtc
            .Select(ToVietnamDate)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        if (distinctVnDates.Count == 1)
            return 1;

        var maxStreak = 1;
        var currentStreak = 1;

        for (var i = 1; i < distinctVnDates.Count; i++)
        {
            if (distinctVnDates[i] == distinctVnDates[i - 1].AddDays(1))
            {
                currentStreak++;
                maxStreak = Math.Max(maxStreak, currentStreak);
            }
            else
            {
                currentStreak = 1;
            }
        }

        return maxStreak;
    }

    internal static DateOnly ToVietnamDate(DateTime utcTimestamp)
    {
        var utc = utcTimestamp.Kind switch
        {
            DateTimeKind.Utc => utcTimestamp,
            DateTimeKind.Local => utcTimestamp.ToUniversalTime(),
            _ => DateTime.SpecifyKind(utcTimestamp, DateTimeKind.Utc)
        };

        var vnLocal = TimeZoneInfo.ConvertTimeFromUtc(utc, VietnamTimeZone);
        return DateOnly.FromDateTime(vnLocal);
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }
}
