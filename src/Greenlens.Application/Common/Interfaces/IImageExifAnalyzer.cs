namespace Greenlens.Application.Common.Interfaces;

/// <summary>BR-REP-011: extract EXIF timestamp/GPS from report images for data-quality checks.</summary>
public interface IImageExifAnalyzer
{
    ImageExifAnalysis Analyze(
        ReadOnlyMemory<byte> imageBytes,
        decimal submittedLatitude,
        decimal submittedLongitude);
}

public sealed record ImageExifAnalysis(
    bool HasTimestamp,
    DateTime? CapturedAtUtc,
    decimal? Latitude,
    decimal? Longitude,
    string? ExifJson,
    IReadOnlyList<string> SuspiciousReasons)
{
    public bool IsSuspicious => SuspiciousReasons.Count > 0;
}
