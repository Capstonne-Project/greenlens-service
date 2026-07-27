using Greenlens.Application.Features.Gamification;

namespace Greenlens.Application.UnitTests;

public sealed class ReportStreakCalculatorTests
{
    [Fact]
    public void ComputeMaxConsecutiveDays_Empty_ReturnsZero()
    {
        var result = ReportStreakCalculator.ComputeMaxConsecutiveDays([]);

        Assert.Equal(0, result);
    }

    [Fact]
    public void ComputeMaxConsecutiveDays_SingleSubmit_ReturnsOne()
    {
        var utc = new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc);

        var result = ReportStreakCalculator.ComputeMaxConsecutiveDays([utc]);

        Assert.Equal(1, result);
    }

    [Fact]
    public void ComputeMaxConsecutiveDays_SevenConsecutiveVnDays_ReturnsSeven_BR_GAM_004()
    {
        // 2026-07-21..27 VN at noon local ≈ 05:00 UTC
        var timestamps = Enumerable.Range(0, 7)
            .Select(i => new DateTime(2026, 7, 21 + i, 5, 0, 0, DateTimeKind.Utc))
            .ToList();

        var result = ReportStreakCalculator.ComputeMaxConsecutiveDays(timestamps);

        Assert.Equal(7, result);
    }

    [Fact]
    public void ComputeMaxConsecutiveDays_UtcMidnightCrossesVnDate_CountsTwoDays_BR_GAM_004()
    {
        // 2026-07-27 17:30 UTC = 2026-07-28 00:30 VN
        // 2026-07-28 10:00 UTC = 2026-07-28 17:00 VN → same VN day
        var timestamps = new[]
        {
            new DateTime(2026, 7, 27, 17, 30, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc)
        };

        var result = ReportStreakCalculator.ComputeMaxConsecutiveDays(timestamps);

        Assert.Equal(1, result);
    }

    [Fact]
    public void ComputeMaxConsecutiveDays_GapBreaksStreak_ReturnsLongestRun_BR_GAM_004()
    {
        var timestamps = new[]
        {
            new DateTime(2026, 7, 1, 5, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 2, 5, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 4, 5, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 5, 5, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 6, 5, 0, 0, DateTimeKind.Utc)
        };

        var result = ReportStreakCalculator.ComputeMaxConsecutiveDays(timestamps);

        Assert.Equal(3, result);
    }
}
