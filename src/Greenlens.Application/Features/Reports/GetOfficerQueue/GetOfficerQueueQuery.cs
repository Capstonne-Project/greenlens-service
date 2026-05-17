using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetOfficerQueue;

/// <summary>Query officer's report queue. BR-OFF-010.</summary>
public sealed record GetOfficerQueueQuery(
    int Page = 1,
    int PageSize = 20,
    ReportStatus? StatusFilter = null) : IRequest<Result<GetOfficerQueueResponse>>;

public sealed record GetOfficerQueueResponse(
    IReadOnlyList<OfficerQueueItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

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
    DateTime? SlaResolveDueAt);
