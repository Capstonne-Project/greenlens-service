using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetCompanyQueue;

/// <summary>
/// CompanyManager views reports dispatched to their company (Status == Verified, AssignedCompanyId == myCompanyId).
/// </summary>
public sealed record GetCompanyQueueQuery(
    int Page = 1,
    int PageSize = 20,
    Severity? Severity = null) : IRequest<Result<GetCompanyQueueResponse>>;

public sealed record GetCompanyQueueResponse(
    List<CompanyQueueItem> Items,
    PaginationMeta Pagination);

public sealed record CompanyQueueItem(
    Guid ReportId,
    string Code,
    string? Address,
    string? WardCode,
    decimal Latitude,
    decimal Longitude,
    string CategoryName,
    Severity Severity,
    DateTime? DispatchedAt,
    DateTime? SlaResolveDueAt);
