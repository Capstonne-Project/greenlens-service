using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetReopenRequests;

/// <summary>LEO queue of citizen reopen requests (BR-REP-015).</summary>
public sealed record GetReopenRequestsQuery(
    int Page = 1,
    int PageSize = 20,
    ReopenRequestStatus? Status = ReopenRequestStatus.Pending) : IRequest<Result<GetReopenRequestsResponse>>;

public sealed record GetReopenRequestsResponse(
    IReadOnlyList<ReopenRequestListItem> Items,
    PaginationMeta Pagination);

public sealed record ReopenRequestListItem(
    Guid RequestId,
    Guid ReportId,
    string ReportCode,
    ReportStatus ReportStatus,
    string Reason,
    ReopenRequestStatus Status,
    DateTime RequestedAt,
    string? FirstEvidenceImageUrl,
    int EvidenceImageCount,
    bool HasVideo);
