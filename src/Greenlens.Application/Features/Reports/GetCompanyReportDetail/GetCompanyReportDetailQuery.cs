using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetCompanyReportDetail;

/// <summary>
/// CompanyManager views detailed progress/timeline of a specific report
/// dispatched to their company, including all team assignments and status history.
/// </summary>
public sealed record GetCompanyReportDetailQuery(Guid ReportId) : IRequest<Result<CompanyReportDetailResponse>>;

public sealed record CompanyReportDetailResponse(
    // ── Report info ──
    Guid ReportId,
    string Code,
    ReportStatus Status,
    Severity Severity,
    string CategoryName,
    string? Description,
    string? Address,
    string? WardCode,
    decimal Latitude,
    decimal Longitude,
    DateTime CreatedAt,
    DateTime? DispatchedToCompanyAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    int ReopenedCount,
    // ── SLA ──
    CompanyReportSlaInfo Sla,
    // ── Aggregate summary ──
    CompanyReportProgressSummary Summary,
    // ── Media grouped by phase ──
    CompanyReportMediaGroup Media,
    // ── All team assignments for this report (company teams only) ──
    IReadOnlyList<CompanyReportTeamAssignment> TeamAssignments,
    // ── Full status timeline ──
    IReadOnlyList<CompanyReportTimelineEntry> Timeline,
    // ── Waste tags ──
    IReadOnlyList<CompanyReportWasteTag> WasteTags);

// ── SLA ──

/// <summary>SLA countdown — HoursRemaining is negative when breached.</summary>
public sealed record CompanyReportSlaInfo(
    DateTime? ResolveDueAt,
    int? HoursRemaining,
    bool IsBreached,
    string SeverityLabel);

// ── Progress summary ──

/// <summary>Aggregated team stats across all company assignments on this report.</summary>
public sealed record CompanyReportProgressSummary(
    int TotalTeams,
    int AcceptedTeams,
    int CompletedTeams,
    int DeclinedTeams,
    int PendingTeams,
    int OverallProgressPercent,
    DateTime? StartedAt);

// ── Media grouped by phase ──

/// <summary>Report media grouped: Before (citizen), Progress (team mid-task), After (completion).</summary>
public sealed record CompanyReportMediaGroup(
    IReadOnlyList<CompanyReportMediaItem> BeforeImages,
    IReadOnlyList<CompanyReportMediaItem> ProgressImages,
    IReadOnlyList<CompanyReportMediaItem> AfterImages);

public sealed record CompanyReportMediaItem(
    string Url,
    DateTime UploadedAt);

// ── Team assignments ──

public sealed record CompanyReportTeamAssignment(
    Guid AssignmentId,
    AssignmentStatus Status,
    DateTime AssignedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? Note,
    string? DeclineReason,
    // ── Progress ──
    int ProgressPercent,
    string? ProgressNote,
    DateTime? ProgressUpdatedAt,
    string? ProgressUpdatedByName,
    // ── Team ──
    Guid TeamId,
    string TeamName,
    IReadOnlyList<CompanyReportTeamMember> Members,
    // ── Assigned by ──
    string AssignedByName);

public sealed record CompanyReportTeamMember(
    Guid UserId,
    string FullName,
    bool IsLeader);

// ── Timeline ──

public sealed record CompanyReportTimelineEntry(
    DateTime Timestamp,
    ReportStatus? FromStatus,
    ReportStatus ToStatus,
    string? ChangedByName,
    string? Reason);

// ── Waste tags ──

public sealed record CompanyReportWasteTag(
    Guid TagId,
    string Code,
    string NameVi,
    string? IconUrl);
