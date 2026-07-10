using Greenlens.Application.Common.Models;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Reports.GetDepartmentReports;

/// <summary>Response containing paged reports for DEO Dashboard.</summary>
public sealed record GetDepartmentReportsResponse(
    Guid DepartmentId,
    string DepartmentName,
    IReadOnlyList<DepartmentReportItem> Items,
    PaginationMeta Pagination);

/// <summary>Single report row for the DEO dashboard table.</summary>
public sealed record DepartmentReportItem(
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
    string? WardName,
    Guid? ReporterId,
    string? ReporterName,
    Guid? AssignedOfficeId,
    string? AssignedOfficeName,
    int AssignmentCount,
    decimal PriorityScore,
    int ReporterCount,
    int ReopenedCount,
    DateTime CreatedAt,
    DateTime? VerifiedAt,
    DateTime? StartedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    DateTime? SlaVerifyDueAt,
    DateTime? SlaResolveDueAt,
    string? FirstImageUrl);
