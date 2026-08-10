using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetReportProgress;

public sealed record GetReportProgressQuery(Guid ReportId) : IRequest<Result<ReportProgressResponse>>;

/// <summary>LEO view: full progress breakdown of a report with a single assigned team.</summary>
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
    AssignedCompanyDto? AssignedCompany,
    AssignmentProgressDto? Assignment,
    ReportMediaGroupDto Media,
    IReadOnlyList<StatusHistoryItemDto> StatusHistory);

/// <summary>Company dispatched by LEO when report is handled by a company team.</summary>
public sealed record AssignedCompanyDto(
    Guid CompanyId,
    string CompanyName,
    DateTime? DispatchedAt);

/// <summary>SLA countdown — HoursRemaining is negative when breached.</summary>
public sealed record SlaInfoDto(
    DateTime? ResolveDueAt,
    int? HoursRemaining,
    bool IsBreached,
    string SeverityLabel);

/// <summary>The single team assignment on this report.</summary>
public sealed record AssignmentProgressDto(
    Guid AssignmentId,
    Guid TeamId,
    string TeamName,
    string TeamType,
    bool IsCompanyTeam,
    Guid? CompanyId,
    string? CompanyName,
    Guid? LocalOfficeId,
    string? LocalOfficeName,
    string? TeamLeaderName,
    Guid AssignedById,
    string AssignedByName,
    string Status,
    DateTime AssignedAt,
    DateTime? AcceptedAt,
    DateTime? CompletedAt,
    string? DeclineReason,
    int ProgressPercent,
    string? ProgressNote,
    DateTime? ProgressUpdatedAt,
    IReadOnlyList<AssignmentTeamMemberDto> Members,
    IReadOnlyList<ProgressUpdateItemDto> ProgressUpdates);

/// <summary>Member of the assigned team (community or company).</summary>
public sealed record AssignmentTeamMemberDto(
    Guid UserId,
    string? FullName,
    string? Email,
    string? PhoneNumber,
    string? AvatarUrl,
    bool IsLeader,
    DateTime JoinedAt);

/// <summary>One progress update snapshot (percent, note, images) from a team leader.</summary>
public sealed record ProgressUpdateItemDto(
    Guid Id,
    int ProgressPercent,
    string? ProgressNote,
    DateTime UpdatedAt,
    Guid UpdatedByUserId,
    string? UpdatedByName,
    IReadOnlyList<MediaItemDto> Images);

/// <summary>Report media grouped by phase (progress images live under assignment.progressUpdates).</summary>
public sealed record ReportMediaGroupDto(
    IReadOnlyList<MediaItemDto> SubmissionImages,
    IReadOnlyList<MediaItemDto> BeforeImages,
    IReadOnlyList<MediaItemDto> AfterImages,
    IReadOnlyList<MediaItemDto> InspectionImages,
    IReadOnlyList<MediaItemDto> ReopenEvidenceImages);

public sealed record MediaItemDto(
    Guid Id,
    string MediaType,
    string Url,
    string? ThumbnailUrl,
    string MimeType,
    long SizeBytes,
    DateTime UploadedAt);

public sealed record StatusHistoryItemDto(
    ReportStatus? FromStatus,
    ReportStatus ToStatus,
    DateTime ChangedAt,
    string? ChangedByName,
    string? Note);
