using Greenlens.Application.Features.Reports;

namespace Greenlens.Application.UnitTests;

public sealed class ExifSuspicionEvaluatorTests
{
    [Fact]
    public void IsTimestampStale_WhenOlderThanOneHour_ReturnsTrue_BR_REP_011()
    {
        var captured = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc);
        var submitted = captured.AddHours(2);

        Assert.True(ExifSuspicionEvaluator.IsTimestampStale(captured, submitted));
    }

    [Fact]
    public void IsTimestampStale_WhenWithinOneHour_ReturnsFalse_BR_REP_011()
    {
        var captured = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc);
        var submitted = captured.AddMinutes(30);

        Assert.False(ExifSuspicionEvaluator.IsTimestampStale(captured, submitted));
    }

    [Fact]
    public void ResolveSuspiciousReason_NoTimestamp_ReturnsMissing_BR_REP_011()
    {
        var reason = ExifSuspicionEvaluator.ResolveSuspiciousReason(
            false, null, DateTime.UtcNow);

        Assert.Equal(ExifSuspicionEvaluator.MissingMetadataReason, reason);
    }

    [Fact]
    public void ResolveSuspiciousReason_StaleTimestamp_ReturnsStale_BR_REP_011()
    {
        var captured = DateTime.UtcNow.AddHours(-3);
        var reason = ExifSuspicionEvaluator.ResolveSuspiciousReason(
            true, captured, DateTime.UtcNow);

        Assert.Equal(ExifSuspicionEvaluator.StaleTimestampReason, reason);
    }
}
