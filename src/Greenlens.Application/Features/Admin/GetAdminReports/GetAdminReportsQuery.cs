using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Admin.GetAdminReports;

public sealed record GetAdminReportsQuery(
    int Page = 1, int PageSize = 20,
    ReportStatus? Status = null,
    Guid? CategoryId = null,
    string? WardCode = null,
    string? ProvinceCode = null,
    string? Search = null) : IRequest<Result<GetAdminReportsResponse>>;

public sealed record GetAdminReportsResponse(
    IReadOnlyList<AdminReportItem> Items, int TotalCount, int Page, int PageSize);

public sealed record AdminReportItem(
    Guid Id, string Code, string CategoryCode, string CategoryName,
    Severity Severity, ReportStatus Status,
    decimal Latitude, decimal Longitude, string? Address,
    string? WardCode, string? ProvinceCode,
    Guid? ReporterId, bool IsAnonymous,
    Guid? AssignedOfficerId, int AssignmentCount,
    decimal PriorityScore, int ReporterCount, int ReopenedCount,
    DateTime CreatedAt, DateTime? VerifiedAt, DateTime? ResolvedAt, DateTime? ClosedAt);
