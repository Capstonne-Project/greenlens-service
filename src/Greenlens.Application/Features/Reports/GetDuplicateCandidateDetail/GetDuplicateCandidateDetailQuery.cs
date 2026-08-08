using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetDuplicateCandidateDetail;

/// <summary>LEO side-by-side detail for a single possible-duplicate report. BR-REP-031, BR-REP-032.</summary>
public sealed record GetDuplicateCandidateDetailQuery(Guid ReportId)
    : IRequest<Result<DuplicateCandidateDetailResponse>>;

public sealed record DuplicateCandidateDetailResponse(
    DuplicateCandidateReportSide Report,
    DuplicateCandidateReportSide PrimaryReport,
    string? DuplicateDetectionSource,
    decimal? AiSimilarityScore,
    double DistanceMeters,
    double HoursSincePrimaryCreated);

public sealed record DuplicateCandidateReportSide(
    Guid Id,
    string Code,
    ReportStatus Status,
    string CategoryCode,
    string CategoryName,
    Severity Severity,
    string? Description,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    DateTime CreatedAt,
    IReadOnlyList<DuplicateCandidateMediaItem> Media);

public sealed record DuplicateCandidateMediaItem(
    Guid Id,
    string Url,
    string? ThumbnailUrl,
    MediaType Type,
    DateTime UploadedAt);
