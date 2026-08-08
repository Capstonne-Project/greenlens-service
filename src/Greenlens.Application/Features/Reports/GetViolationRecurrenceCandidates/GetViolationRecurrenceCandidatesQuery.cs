using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Application.Features.Reports.GetOfficerQueue;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetViolationRecurrenceCandidates;

/// <summary>LEO review list of reports flagged as suspected violation recurrence. BR-REP-034.</summary>
public sealed record GetViolationRecurrenceCandidatesQuery(
    int Page = 1,
    int PageSize = 20,
    ReportStatus? Status = null,
    Severity? Severity = null,
    Guid? CategoryId = null,
    string? WardCode = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Search = null,
    int? MinDaysSincePriorClosed = null,
    int? MaxDaysSincePriorClosed = null,
    ViolationRecurrenceCandidateSortBy SortBy = ViolationRecurrenceCandidateSortBy.CreatedAt,
    SortDirection SortDir = SortDirection.Desc) : IRequest<Result<GetViolationRecurrenceCandidatesResponse>>;

public enum ViolationRecurrenceCandidateSortBy
{
    CreatedAt,
    Severity,
    PriorClosedAt,
    PriorityScore
}

public sealed record GetViolationRecurrenceCandidatesResponse(
    IReadOnlyList<ViolationRecurrenceCandidateItem> Items,
    PaginationMeta Pagination);

public sealed record ViolationRecurrenceCandidateItem(
    Guid Id,
    string Code,
    string CategoryName,
    Severity Severity,
    ReportStatus Status,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    DateTime CreatedAt,
    IReadOnlyList<ReportReviewMediaItem> Media,
    ViolationRecurrencePriorReport? PriorClosedReport);

public sealed record ViolationRecurrencePriorReport(
    Guid Id,
    string Code,
    string? Address,
    ReportStatus Status,
    DateTime? ClosedAt,
    int? DaysSinceClosed,
    IReadOnlyList<ReportReviewMediaItem> Media);
