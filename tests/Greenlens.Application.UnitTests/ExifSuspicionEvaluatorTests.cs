using Greenlens.Application.Features.Reports;

namespace Greenlens.Application.UnitTests;

public sealed class ExifSuspicionEvaluatorTests
{
    [Fact]
    public void ResolveSuspiciousReasons_NoExifGps_ReturnsEmpty_BR_REP_011()
    {
        var reasons = ExifSuspicionEvaluator.ResolveSuspiciousReasons(
            10.7626m, 106.6602m, null, null);

        Assert.Empty(reasons);
    }

    [Fact]
    public void ResolveSuspiciousReasons_GpsFarFromSubmitted_ReturnsGpsMismatch_BR_REP_011()
    {
        var reasons = ExifSuspicionEvaluator.ResolveSuspiciousReasons(
            submittedLatitude: 10.7626m,
            submittedLongitude: 106.6602m,
            exifLatitude: 10.8000m,
            exifLongitude: 106.6602m);

        Assert.Equal([ExifSuspicionEvaluator.GpsMismatchReason], reasons);
    }

    [Fact]
    public void ResolveSuspiciousReasons_GpsNearSubmitted_NoGpsMismatch_BR_REP_011()
    {
        var reasons = ExifSuspicionEvaluator.ResolveSuspiciousReasons(
            submittedLatitude: 10.7626m,
            submittedLongitude: 106.6602m,
            exifLatitude: 10.7627m,
            exifLongitude: 106.6602m);

        Assert.Empty(reasons);
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
                "Vị trí người dân gửi báo cáo trên bản đồ khác với vị trí trong ảnh",
                "Ảnh chụp quá lâu (hơn 1 ngày) so với thời gian gửi"
            ],
            messages);
    }
}
