using Greenlens.Application.Common.Models;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Reports.GetOfficeReports;

/// <summary>Response containing paged reports with team progress for LEO Dashboard.</summary>
public sealed record GetOfficeReportsResponse(
    Guid LocalOfficeId,
    string LocalOfficeName,
    string? WardCode,
    string? WardName,
    IReadOnlyList<OfficeReportItem> Items,
    PaginationMeta Pagination);

/// <summary>Single report row for the LEO dashboard table — includes team assignment progress.</summary>
public sealed record OfficeReportItem(
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
    Guid? ReporterId,
    string? ReporterName,
    string? Description,
    int AssignmentCount,
    decimal PriorityScore,
    int ReporterCount,
    int ReopenedCount,
    int OverallProgressPercent,
    DateTime CreatedAt,
    DateTime? VerifiedAt,
    DateTime? StartedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    DateTime? SlaResolveDueAt,
    /// <summary>First report image URL (ThumbnailUrl ?? Url). Empty when the report has no Image media.</summary>
    IReadOnlyList<string> Thumbnails,
    /// <summary>Company dispatched after reopen / new cycle (BR-CMP-005).</summary>
    OfficeAssignedCompanyItem? AssignedCompany,
    /// <summary>Current-cycle assignment only (newest open row after reopen re-assign).</summary>
    IReadOnlyList<AssignmentProgressItem> Assignments);

/// <summary>Environmental service company dispatched by LEO for this report.</summary>
public sealed record OfficeAssignedCompanyItem(
    Guid CompanyId,
    string CompanyName,
    DateTime? DispatchedAt);

/// <summary>Team assignment progress detail for a report.</summary>
public sealed record AssignmentProgressItem(
    Guid AssignmentId,
    Guid TeamId,
    string TeamName,
    string TeamType,
    AssignmentStatus Status,
    int ProgressPercent,
    string? ProgressNote,
    string? Note,
    string? DeclineReason,
    DateTime AssignedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime? ProgressUpdatedAt);
