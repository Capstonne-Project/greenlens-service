using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetReports;

public sealed record GetReportsQuery(
    int Page = 1, int PageSize = 20,
    ReportStatus? Status = null,
    Guid? CategoryId = null,
    string? WardCode = null,
    Severity? Severity = null) : IRequest<Result<GetReportsResponse>>;

public sealed record GetReportsResponse(
    IReadOnlyList<ReportListItem> Items, int TotalCount, int Page, int PageSize);

public sealed record ReportListItem(
    Guid Id, string Code, string CategoryCode, string CategoryName,
    Severity Severity, ReportStatus Status, decimal Latitude, decimal Longitude,
    string? Address, string? WardCode, int ReporterCount,
    DateTime CreatedAt, DateTime? ResolvedAt);
