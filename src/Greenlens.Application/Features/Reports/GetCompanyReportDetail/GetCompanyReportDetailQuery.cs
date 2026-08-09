using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetCompanyReportDetail;

/// <summary>
/// CompanyManager views detailed progress/timeline of a specific report
/// dispatched to their company, including the assigned team and status history.
/// </summary>
public sealed record GetCompanyReportDetailQuery(Guid ReportId) : IRequest<Result<CompanyReportDetailResponse>>;

public sealed record CompanyReportDetailResponse(
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
    CompanyReportSlaInfo Sla,
    CompanyReportMediaGroup Media,
    CompanyReportTeamAssignment? Assignment,
    IReadOnlyList<CompanyReportTimelineEntry> Timeline,
    IReadOnlyList<CompanyReportWasteTag> WasteTags);

/// <summary>SLA countdown — HoursRemaining is negative when breached.</summary>
public sealed record CompanyReportSlaInfo(
    DateTime? ResolveDueAt,
    int? HoursRemaining,
    bool IsBreached,
    string SeverityLabel);

/// <summary>Report media grouped by phase (progress images live under assignment.progressUpdates).</summary>
public sealed record CompanyReportMediaGroup(
    IReadOnlyList<CompanyReportMediaItem> BeforeImages,
    IReadOnlyList<CompanyReportMediaItem> AfterImages);

public sealed record CompanyReportMediaItem(
    string Url,
    DateTime UploadedAt);

/// <summary>The single company team assignment on this report.</summary>
public sealed record CompanyReportTeamAssignment(
    Guid AssignmentId,
    AssignmentStatus Status,
    DateTime AssignedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? Note,
    string? DeclineReason,
    int ProgressPercent,
    string? ProgressNote,
    DateTime? ProgressUpdatedAt,
    string? ProgressUpdatedByName,
    Guid TeamId,
    string TeamName,
    IReadOnlyList<CompanyReportTeamMember> Members,
    string AssignedByName,
    IReadOnlyList<CompanyReportProgressUpdateItem> ProgressUpdates);

public sealed record CompanyReportTeamMember(
    Guid UserId,
    string FullName,
    bool IsLeader);

public sealed record CompanyReportProgressUpdateItem(
    Guid Id,
    int ProgressPercent,
    string? ProgressNote,
    DateTime UpdatedAt,
    Guid UpdatedByUserId,
    string? UpdatedByName,
    IReadOnlyList<CompanyReportMediaItem> Images);

public sealed record CompanyReportTimelineEntry(
    DateTime Timestamp,
    ReportStatus? FromStatus,
    ReportStatus ToStatus,
    string? ChangedByName,
    string? Reason);

public sealed record CompanyReportWasteTag(
    Guid TagId,
    string Code,
    string NameVi,
    string? IconUrl);
