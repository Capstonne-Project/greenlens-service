namespace Greenlens.Application.Features.Reports;

/// <summary>BR-REP-011: pure rules for marking reports suspicious based on EXIF metadata.</summary>
public static class ExifSuspicionEvaluator
{
    public const string StaleTimestampReason = "EXIF_TIMESTAMP_STALE";
    public const string MissingMetadataReason = "EXIF_METADATA_MISSING";
    public const string StaleWarningMessage = "Ảnh có thể không phản ánh hiện trạng thực tế";

    public static bool IsTimestampStale(DateTime capturedAtUtc, DateTime submittedAtUtc) =>
        submittedAtUtc - capturedAtUtc > TimeSpan.FromHours(1);

    public static string? ResolveSuspiciousReason(bool hasTimestamp, DateTime? capturedAtUtc, DateTime submittedAtUtc)
    {
        if (!hasTimestamp || capturedAtUtc is null)
            return MissingMetadataReason;

        return IsTimestampStale(capturedAtUtc.Value, submittedAtUtc)
            ? StaleTimestampReason
            : null;
    }
}
