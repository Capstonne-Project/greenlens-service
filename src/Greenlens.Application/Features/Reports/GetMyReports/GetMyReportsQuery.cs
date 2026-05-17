using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetMyReports;

public sealed record GetMyReportsQuery(
    int Page = 1, int PageSize = 20,
    ReportStatus? Status = null) : IRequest<Result<GetMyReportsResponse>>;

public sealed record GetMyReportsResponse(
    IReadOnlyList<MyReportItem> Items, int TotalCount, int Page, int PageSize);

public sealed record MyReportItem(
    Guid Id, string Code, string CategoryName,
    Severity Severity, ReportStatus Status,
    string? Address, DateTime CreatedAt,
    DateTime? ResolvedAt, DateTime? ClosedAt);
