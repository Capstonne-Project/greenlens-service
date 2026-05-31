using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetReportProgress;

public sealed record GetReportProgressQuery(Guid ReportId) : IRequest<Result<ReportProgressResponse>>;

/// <summary>LEO view: full progress breakdown of an InProgress report.</summary>
public sealed record ReportProgressResponse(
    Guid ReportId,
    string Code,
    ReportStatus Status,
    Severity Severity,
    string CategoryName,
    string? Address,
    string? WardCode,
    string? Description,
    SlaInfoDto Sla,
    ProgressSummaryDto Summary,
    IReadOnlyList<AssignmentProgressDto> Assignments,
    ReportMediaGroupDto Media,
    IReadOnlyList<StatusHistoryItemDto> StatusHistory);

/// <summary>SLA countdown — HoursRemaining is negative when breached.</summary>
public sealed record SlaInfoDto(
    DateTime? ResolveDueAt,
    int? HoursRemaining,
    bool IsBreached,
    string SeverityLabel);

/// <summary>Aggregated team stats across all assignments on this report.</summary>
public sealed record ProgressSummaryDto(
    int TotalTeams,
    int AcceptedTeams,
    int CompletedTeams,
    int DeclinedTeams,
    int PendingTeams,
    int OverallProgressPercent,
    DateTime? StartedAt);

/// <summary>Per-team assignment detail for LEO to monitor each team's work.</summary>
public sealed record AssignmentProgressDto(
    Guid AssignmentId,
    Guid TeamId,
    string TeamName,
    string TeamType,
    string? TeamLeaderName,
    string Status,
    DateTime AssignedAt,
    DateTime? AcceptedAt,
    DateTime? CompletedAt,
    string? DeclineReason,
    int ProgressPercent,
    string? ProgressNote,
    DateTime? ProgressUpdatedAt);

/// <summary>Report media grouped by phase: Before (citizen), Progress (team mid-task), After (completion evidence).</summary>
public sealed record ReportMediaGroupDto(
    IReadOnlyList<MediaItemDto> BeforeImages,
    IReadOnlyList<MediaItemDto> ProgressImages,
    IReadOnlyList<MediaItemDto> AfterImages);

public sealed record MediaItemDto(string Url, DateTime UploadedAt);

public sealed record StatusHistoryItemDto(
    ReportStatus? FromStatus,
    ReportStatus ToStatus,
    DateTime ChangedAt,
    string? ChangedByName,
    string? Note);
