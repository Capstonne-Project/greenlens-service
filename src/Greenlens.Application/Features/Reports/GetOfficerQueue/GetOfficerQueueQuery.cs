using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetOfficerQueue;

/// <summary>Query officer's report queue. BR-OFF-010.</summary>
public sealed record GetOfficerQueueQuery(
    int Page = 1,
    int PageSize = 20,
    // ── Filters ──
    ReportStatus? StatusFilter = null,
    Severity? SeverityFilter = null,
    Guid? CategoryId = null,
    string? WardCode = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    bool? SlaBreached = null,
    // ── Search ──
    string? Search = null,
    // ── Sort ──
    QueueSortBy SortBy = QueueSortBy.PriorityScore,
    SortDirection SortDir = SortDirection.Desc) : IRequest<Result<GetOfficerQueueResponse>>;

/// <summary>Sort fields for officer queue.</summary>
public enum QueueSortBy
{
    PriorityScore,
    CreatedAt,
    Severity,
    SlaVerifyDueAt,
    SlaResolveDueAt
}

/// <summary>Sort direction.</summary>
public enum SortDirection
{
    Asc,
    Desc
}

public sealed record GetOfficerQueueResponse(
    IReadOnlyList<OfficerQueueItem> Items,
    PaginationMeta Pagination);

public sealed record OfficerQueueItem(
    Guid Id,
    string Code,
    string CategoryCode,
    string CategoryName,
    Severity Severity,
    ReportStatus Status,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    string? WardCode,
    decimal PriorityScore,
    DateTime CreatedAt,
    DateTime? SlaVerifyDueAt,
    DateTime? SlaResolveDueAt,
    string? FirstImageUrl);
