using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetDuplicateCandidates;

/// <summary>LEO review list of reports flagged as possible duplicates. BR-REP-031, BR-REP-032.</summary>
public sealed record GetDuplicateCandidatesQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<Result<GetDuplicateCandidatesResponse>>;

public sealed record GetDuplicateCandidatesResponse(
    IReadOnlyList<DuplicateCandidateItem> Items,
    PaginationMeta Pagination);

public sealed record DuplicateCandidateItem(
    Guid Id,
    string Code,
    string CategoryName,
    Severity Severity,
    ReportStatus Status,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    DateTime CreatedAt,
    string? DuplicateDetectionSource,
    decimal? AiSimilarityScore,
    IReadOnlyList<ReportReviewMediaItem> Media,
    DuplicateCandidatePrimary? Primary);

public sealed record DuplicateCandidatePrimary(
    Guid Id,
    string Code,
    string? Address,
    DateTime CreatedAt,
    IReadOnlyList<ReportReviewMediaItem> Media);
