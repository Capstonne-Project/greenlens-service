using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetCompanyQueue;

/// <summary>
/// CompanyManager views reports dispatched to their company awaiting team assignment.
/// Filters: Status == InProgress, AssignedCompanyId == myCompanyId, no active assignments.
/// </summary>
public sealed record GetCompanyQueueQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Severity? Severity = null,
    string? WardCode = null,
    Guid? CategoryId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? SortBy = null,
    bool SortDesc = false) : IRequest<Result<GetCompanyQueueResponse>>;

public sealed record GetCompanyQueueResponse(
    List<CompanyQueueItem> Items,
    PaginationMeta Pagination);

public sealed record CompanyQueueItem(
    Guid ReportId,
    string Code,
    string? Address,
    string? WardCode,
    string? ProvinceCode,
    decimal Latitude,
    decimal Longitude,
    string CategoryName,
    Severity Severity,
    DateTime? DispatchedAt,
    DateTime? VerifiedAt,
    string? VerifiedByName,
    DateTime? SlaResolveDueAt,
    IReadOnlyList<ReportReviewMediaItem> Media);
