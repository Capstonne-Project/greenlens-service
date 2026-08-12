using Greenlens.Application.Common;

namespace Greenlens.Application.Features.Reports;

/// <summary>BR-REP-011: pure rules for marking reports suspicious based on EXIF GPS metadata.</summary>
public static class ExifSuspicionEvaluator
{
    public const string StaleTimestampReason = "EXIF_TIMESTAMP_STALE";
    public const string MissingMetadataReason = "EXIF_METADATA_MISSING";
    public const string GpsMismatchReason = "EXIF_GPS_MISMATCH";
    public const string ExifWarningMessage = "Ảnh có thể không phản ánh hiện trạng thực tế";

    /// <summary>Legacy alias for submit response.</summary>
    public const string StaleWarningMessage = ExifWarningMessage;

    /// <summary>Max distance (m) between submitted pin and EXIF GPS before flagging (BR-CLN-002 parity).</summary>
    public const double GpsMismatchThresholdMeters = 200.0;

    public static bool IsGpsMismatch(
        decimal submittedLatitude,
        decimal submittedLongitude,
        decimal exifLatitude,
        decimal exifLongitude) =>
        GeoMath.HaversineMeters(submittedLatitude, submittedLongitude, exifLatitude, exifLongitude)
            > GpsMismatchThresholdMeters;

    /// <summary>Returns applicable suspicion reason codes (may be empty). Timestamp is not checked.</summary>
    public static IReadOnlyList<string> ResolveSuspiciousReasons(
        decimal submittedLatitude,
        decimal submittedLongitude,
        decimal? exifLatitude,
        decimal? exifLongitude)
    {
        if (exifLatitude.HasValue && exifLongitude.HasValue
            && IsGpsMismatch(submittedLatitude, submittedLongitude, exifLatitude.Value, exifLongitude.Value))
            return [GpsMismatchReason];

        return [];
    }

    /// <summary>Maps persisted reason codes to officer-facing messages (vi-VN, BR-REP-011).</summary>
    public static string ToDisplayMessage(string reasonCode) =>
        reasonCode switch
        {
            MissingMetadataReason => "Ảnh thiếu thông tin thời gian chụp",
            StaleTimestampReason => "Ảnh chụp quá lâu (hơn 1 ngày) so với thời gian gửi",
            GpsMismatchReason => "Vị trí người dân gửi báo cáo trên bản đồ khác với vị trí trong ảnh",
            _ => reasonCode
        };

    public static IReadOnlyList<string> ToDisplayMessages(IEnumerable<string> reasonCodes) =>
        reasonCodes.Select(ToDisplayMessage).ToList();
}
