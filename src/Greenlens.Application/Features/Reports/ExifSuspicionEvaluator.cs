using Greenlens.Application.Common;

namespace Greenlens.Application.Features.Reports;

/// <summary>BR-REP-011: pure rules for marking reports suspicious based on EXIF metadata.</summary>
public static class ExifSuspicionEvaluator
{
    public const string StaleTimestampReason = "EXIF_TIMESTAMP_STALE";
    public const string MissingMetadataReason = "EXIF_METADATA_MISSING";
    public const string GpsMismatchReason = "EXIF_GPS_MISMATCH";
    public const string StaleWarningMessage = "Ảnh có thể không phản ánh hiện trạng thực tế";

    /// <summary>Max distance (m) between submitted pin and EXIF GPS before flagging (BR-CLN-002 parity).</summary>
    public const double GpsMismatchThresholdMeters = 200.0;

    /// <summary>Photo taken more than one day before submit → stale (BR-REP-011).</summary>
    public static readonly TimeSpan StaleTimestampThreshold = TimeSpan.FromDays(1);

    public static bool IsTimestampStale(DateTime capturedAtUtc, DateTime submittedAtUtc) =>
        submittedAtUtc - capturedAtUtc > StaleTimestampThreshold;

    public static bool IsGpsMismatch(
        decimal submittedLatitude,
        decimal submittedLongitude,
        decimal exifLatitude,
        decimal exifLongitude) =>
        GeoMath.HaversineMeters(submittedLatitude, submittedLongitude, exifLatitude, exifLongitude)
            > GpsMismatchThresholdMeters;

    /// <summary>Returns all applicable suspicion reason codes (may be empty).</summary>
    public static IReadOnlyList<string> ResolveSuspiciousReasons(
        bool hasTimestamp,
        DateTime? capturedAtUtc,
        DateTime submittedAtUtc,
        decimal submittedLatitude,
        decimal submittedLongitude,
        decimal? exifLatitude,
        decimal? exifLongitude)
    {
        var reasons = new List<string>();

        if (!hasTimestamp || capturedAtUtc is null)
            reasons.Add(MissingMetadataReason);
        else if (IsTimestampStale(capturedAtUtc.Value, submittedAtUtc))
            reasons.Add(StaleTimestampReason);

        if (exifLatitude.HasValue && exifLongitude.HasValue
            && IsGpsMismatch(submittedLatitude, submittedLongitude, exifLatitude.Value, exifLongitude.Value))
            reasons.Add(GpsMismatchReason);

        return reasons;
    }

    /// <summary>Maps persisted reason codes to officer-facing messages (vi-VN, BR-REP-011).</summary>
    public static string ToDisplayMessage(string reasonCode) =>
        reasonCode switch
        {
            MissingMetadataReason => "Ảnh thiếu metadata EXIF (thời gian chụp)",
            StaleTimestampReason => "Ảnh chụp hơn 1 ngày trước khi gửi báo cáo",
            GpsMismatchReason => "Vị trí chọn trên bản đồ khác GPS trên ảnh (hơn 200m)",
            _ => reasonCode
        };

    public static IReadOnlyList<string> ToDisplayMessages(IEnumerable<string> reasonCodes) =>
        reasonCodes.Select(ToDisplayMessage).ToList();
}
