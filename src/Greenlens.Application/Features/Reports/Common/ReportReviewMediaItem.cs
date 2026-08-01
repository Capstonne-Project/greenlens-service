using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Reports.Common;

/// <summary>Citizen-submitted image/video on a report (submit flow — BR-REP-001/002).</summary>
public sealed record ReportReviewMediaItem(
    Guid Id,
    string Url,
    string? ThumbnailUrl,
    MediaType Type,
    DateTime UploadedAt);
