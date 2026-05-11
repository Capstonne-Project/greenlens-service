using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Image or video attached to a report. Stores URL, metadata, and AI analysis results.
/// </summary>
/// <remarks>Implements: BR-REP-001, 002, 011 (suspicious detection via pHash).</remarks>
public sealed class ReportMedia : BaseEntity
{
    private ReportMedia() { }

    public Guid ReportId { get; private set; }
    public MediaType Type { get; private set; }
    public string Url { get; private set; } = default!;
    public string? ThumbnailUrl { get; private set; }
    public string MimeType { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public string? PHash { get; private set; }
    public string? ExifData { get; private set; }
    public Guid? UploadedBy { get; private set; }
    public DateTime UploadedAt { get; private set; }

    // ── Navigation ──
    public Report Report { get; private set; } = default!;
    public User? Uploader { get; private set; }

    public static ReportMedia Create(
        Guid reportId,
        MediaType type,
        string url,
        string mimeType,
        long sizeBytes,
        Guid? uploadedBy,
        string? thumbnailUrl = null,
        int? width = null,
        int? height = null,
        string? pHash = null,
        string? exifData = null)
    {
        return new ReportMedia
        {
            ReportId = reportId,
            Type = type,
            Url = url,
            ThumbnailUrl = thumbnailUrl,
            MimeType = mimeType,
            SizeBytes = sizeBytes,
            Width = width,
            Height = height,
            PHash = pHash,
            ExifData = exifData,
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.UtcNow
        };
    }

    public void SetThumbnail(string thumbnailUrl) => ThumbnailUrl = thumbnailUrl;

    public void SetPHash(string pHash) => PHash = pHash;

    public void SetExifData(string exifData) => ExifData = exifData;

    public void SetDimensions(int width, int height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>Change media type (e.g. IMAGE → BEFORE/AFTER during resolve).</summary>
    public void ChangeType(MediaType newType) => Type = newType;
}
