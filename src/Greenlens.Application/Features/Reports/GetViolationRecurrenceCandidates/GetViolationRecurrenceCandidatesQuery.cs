using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetViolationRecurrenceCandidates;

/// <summary>LEO review list of reports flagged as suspected violation recurrence. BR-REP-034.</summary>
public sealed record GetViolationRecurrenceCandidatesQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<Result<GetViolationRecurrenceCandidatesResponse>>;

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
    string? FirstImageUrl,
    ViolationRecurrencePriorReport? PriorClosedReport);

public sealed record ViolationRecurrencePriorReport(
    Guid Id,
    string Code,
    string? Address,
    ReportStatus Status,
    DateTime? ClosedAt,
    int? DaysSinceClosed);
