using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Application.Features.Reports.GetDuplicateCandidates;
using Greenlens.Application.Features.Reports.GetOfficerQueue;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetDuplicateCandidatesV2;

/// <summary>
/// LEO review list grouped by primary report — each primary includes all flagged duplicates.
/// BR-REP-031, BR-REP-032.
/// </summary>
public sealed record GetDuplicateCandidatesV2Query(
    int Page = 1,
    int PageSize = 20,
    Guid? PrimaryReportId = null,
    ReportStatus? Status = null,
    Severity? Severity = null,
    Guid? CategoryId = null,
    string? WardCode = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Search = null,
    string? DuplicateDetectionSource = null,
    decimal? MinAiSimilarityScore = null,
    DuplicateCandidateSortBy SortBy = DuplicateCandidateSortBy.CreatedAt,
    SortDirection SortDir = SortDirection.Desc) : IRequest<Result<GetDuplicateCandidatesV2Response>>;

public sealed record GetDuplicateCandidatesV2Response(
    IReadOnlyList<DuplicateCandidateGroupItem> Items,
    PaginationMeta Pagination);

public sealed record DuplicateCandidateGroupItem(
    DuplicateCandidatePrimary Primary,
    IReadOnlyList<DuplicateCandidateEntry> Duplicates,
    int DuplicateCount);

public sealed record DuplicateCandidateEntry(
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
    IReadOnlyList<ReportReviewMediaItem> Media);
