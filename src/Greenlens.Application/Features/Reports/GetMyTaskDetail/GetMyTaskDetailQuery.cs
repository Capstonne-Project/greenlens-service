using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetMyTaskDetail;

/// <summary>
/// Returns full task detail for the current user's team on a specific report.
/// TeamId resolved from token — any team member can view, not just leader.
/// </summary>
public sealed record GetMyTaskDetailQuery(Guid ReportId) : IRequest<Result<MyTaskDetailResponse>>;

public sealed record MyTaskDetailResponse(
    // Assignment info
    Guid AssignmentId,
    AssignmentStatus AssignmentStatus,
    DateTime AssignedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    bool CanDecline,
    bool CanUpdateProgress,
    bool CanResolve,

    // Report basic info
    Guid ReportId,
    string ReportCode,
    ReportStatus ReportStatus,
    string CategoryCode,
    string CategoryName,
    Severity Severity,
    string? Description,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    string? WardCode,

    // SLA
    DateTime? SlaResolveDueAt,

    // Original report images (before)
    IReadOnlyList<TaskImageItem> ReportImages,

    // Current progress of this team
    int ProgressPercent,
    string? ProgressNote,
    DateTime? ProgressUpdatedAt,
    Guid? ProgressUpdatedByUserId,

    // Assignment note from officer
    string? AssignmentNote
);

public sealed record TaskImageItem(string Url, string MimeType);
