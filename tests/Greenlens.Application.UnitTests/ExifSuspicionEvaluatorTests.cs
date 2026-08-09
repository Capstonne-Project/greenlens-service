using Greenlens.Application.Features.Reports;

namespace Greenlens.Application.UnitTests;

public sealed class ExifSuspicionEvaluatorTests
{
    [Fact]
    public void IsTimestampStale_WhenOlderThanOneDay_ReturnsTrue_BR_REP_011()
    {
        var captured = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc);
        var submitted = captured.AddDays(2);

        Assert.True(ExifSuspicionEvaluator.IsTimestampStale(captured, submitted));
    }

    [Fact]
    public void IsTimestampStale_WhenWithinOneDay_ReturnsFalse_BR_REP_011()
    {
        var captured = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc);
        var submitted = captured.AddHours(12);

        Assert.False(ExifSuspicionEvaluator.IsTimestampStale(captured, submitted));
    }

    [Fact]
    public void ResolveSuspiciousReasons_NoTimestamp_ReturnsMissing_BR_REP_011()
    {
        var reasons = ExifSuspicionEvaluator.ResolveSuspiciousReasons(
            false, null, DateTime.UtcNow, 10.7626m, 106.6602m, null, null);

        Assert.Equal([ExifSuspicionEvaluator.MissingMetadataReason], reasons);
    }

    [Fact]
    public void ResolveSuspiciousReasons_StaleTimestamp_ReturnsStale_BR_REP_011()
    {
        var captured = DateTime.UtcNow.AddDays(-2);
        var reasons = ExifSuspicionEvaluator.ResolveSuspiciousReasons(
            true, captured, DateTime.UtcNow, 10.7626m, 106.6602m, 10.7626m, 106.6602m);

        Assert.Equal([ExifSuspicionEvaluator.StaleTimestampReason], reasons);
    }

    [Fact]
    public void ResolveSuspiciousReasons_GpsFarFromSubmitted_ReturnsGpsMismatch_BR_REP_011()
    {
        var reasons = ExifSuspicionEvaluator.ResolveSuspiciousReasons(
            true,
            DateTime.UtcNow,
            DateTime.UtcNow,
            submittedLatitude: 10.7626m,
            submittedLongitude: 106.6602m,
            exifLatitude: 10.8000m,
            exifLongitude: 106.6602m);

        Assert.Contains(ExifSuspicionEvaluator.GpsMismatchReason, reasons);
    }

    [Fact]
    public void ResolveSuspiciousReasons_GpsNearSubmitted_NoGpsMismatch_BR_REP_011()
    {
        var reasons = ExifSuspicionEvaluator.ResolveSuspiciousReasons(
            true,
            DateTime.UtcNow,
            DateTime.UtcNow,
            submittedLatitude: 10.7626m,
            submittedLongitude: 106.6602m,
            exifLatitude: 10.7627m,
            exifLongitude: 106.6602m);

        Assert.DoesNotContain(ExifSuspicionEvaluator.GpsMismatchReason, reasons);
    }

    [Fact]
    public void ResolveSuspiciousReasons_StaleAndGpsMismatch_ReturnsBoth_BR_REP_011()
    {
        var captured = DateTime.UtcNow.AddDays(-2);
        var reasons = ExifSuspicionEvaluator.ResolveSuspiciousReasons(
            true,
            captured,
            DateTime.UtcNow,
            submittedLatitude: 10.7626m,
            submittedLongitude: 106.6602m,
            exifLatitude: 10.8000m,
            exifLongitude: 106.6602m);

        Assert.Contains(ExifSuspicionEvaluator.StaleTimestampReason, reasons);
        Assert.Contains(ExifSuspicionEvaluator.GpsMismatchReason, reasons);
    }
}

public sealed class ReportSuspiciousReasonsParserTests
{
    [Fact]
    public void ToDisplayMessages_MapsKnownCodesToMessages_BR_REP_011()
    {
        var json = "[\"EXIF_GPS_MISMATCH\",\"EXIF_TIMESTAMP_STALE\"]";
        var messages = ReportSuspiciousReasonsParser.ToDisplayMessages(json);

        Assert.Equal(
            [
                "Vị trí người dân chọn trên bản đồ khác với vị trí trong ảnh",
                "Ảnh chụp quá lâu (hơn 1 ngày) so với thời gian gửi"
            ],
            messages);
    }
}
