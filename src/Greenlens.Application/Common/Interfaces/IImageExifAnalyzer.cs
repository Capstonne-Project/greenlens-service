namespace Greenlens.Application.Common.Interfaces;

/// <summary>BR-REP-011: extract EXIF timestamp/GPS from report images for data-quality checks.</summary>
public interface IImageExifAnalyzer
{
    ImageExifAnalysis Analyze(ReadOnlyMemory<byte> imageBytes, DateTime submittedAtUtc);
}

public sealed record ImageExifAnalysis(
    bool HasTimestamp,
    DateTime? CapturedAtUtc,
    decimal? Latitude,
    decimal? Longitude,
    string? ExifJson,
    bool IsSuspicious,
    string? SuspiciousReasonCode);
