namespace Greenlens.Application.Features.Media.UploadReportImage;

/// <summary>
/// Returned after upload so the client can pass <c>url</c>, <c>mimeType</c>, and <c>sizeBytes</c>
/// into the submit-report payload.
/// </summary>
public sealed record UploadReportImageResponse(
    string Url,
    string Key,
    string Message,
    string MimeType,
    long SizeBytes);
