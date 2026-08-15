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

    // Original report images (citizen)
    IReadOnlyList<TaskImageItem> ReportImages,

    // Current progress of this team
    int ProgressPercent,
    string? ProgressNote,
    DateTime? ProgressUpdatedAt,
    Guid? ProgressUpdatedByUserId,

    // Assignment note from officer
    string? AssignmentNote,

    // Waste tags on the report (so team knows what to prepare)
    IReadOnlyList<TaskWasteTagItem> WasteTags,

    // Timing helpers for mobile countdown UI
    /// <summary>AssignedAt + 24h — deadline to decline while still Assigned.</summary>
    DateTime DeclineDeadlineAt,
    /// <summary>True when at least 1 before image exists for the current assignment cycle.</summary>
    bool HasBeforeImages,
    int BeforeImageCount,
    /// <summary>Before cleanup images for the current assignment cycle.</summary>
    IReadOnlyList<TaskImageItem> BeforeImages,
    /// <summary>After cleanup images for the current assignment cycle.</summary>
    IReadOnlyList<TaskImageItem> AfterImages,
    /// <summary>Progress update history for the current assignment (newest last).</summary>
    IReadOnlyList<TaskProgressUpdateItem> ProgressUpdates,
    /// <summary>
    /// Soft SLA: team should update progress at least once / 24h while InProgress.
    /// = (ProgressUpdatedAt ?? StartedAt) + 24h. Null when not InProgress.
    /// </summary>
    DateTime? ProgressRequiredByAt
);

public sealed record TaskImageItem(string Url, string MimeType);

public sealed record TaskProgressUpdateItem(
    Guid Id,
    int ProgressPercent,
    string? ProgressNote,
    DateTime UpdatedAt,
    IReadOnlyList<TaskImageItem> Images);

public sealed record TaskWasteTagItem(
    string Code, string NameVi, string NameEn, string? IconUrl);
