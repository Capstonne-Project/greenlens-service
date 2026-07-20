namespace Greenlens.Application.Features.Media.UploadReportVideo;

/// <summary>
/// Returned after video upload + transcode so the client can pass url/metadata
/// into the submit-report payload.
/// </summary>
public sealed record UploadReportVideoResponse(
    string Url,
    string Key,
    string Message,
    string MimeType,
    long OriginalSizeBytes,
    long CompressedSizeBytes,
    int DurationSeconds,
    int Width,
    int Height);
