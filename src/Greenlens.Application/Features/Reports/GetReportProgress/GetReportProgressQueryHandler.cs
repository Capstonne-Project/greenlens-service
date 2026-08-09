using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetReportProgress;

/// <summary>
/// Returns full progress breakdown of a report for LEO monitoring.
/// Includes per-team assignment status, progress images, after images, and status history.
/// </summary>
/// <remarks>
/// Implements: BR-OFF-020 (SLA countdown), BR-OFF-011 (multi-team tracking).
/// Scope: LEO → assigned office; Admin → all.
/// </remarks>
public sealed class GetReportProgressQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetReportProgressQueryHandler> logger)
    : IRequestHandler<GetReportProgressQuery, Result<ReportProgressResponse>>
{
    private static readonly Dictionary<Severity, string> SlaLabels = new()
    {
        { Severity.Critical, "Critical (3 ngày)" },
        { Severity.High,     "High (5 ngày)" },
        { Severity.Medium,   "Medium (7 ngày)" },
        { Severity.Low,      "Low (10 ngày)" },
    };

    public async Task<Result<ReportProgressResponse>> Handle(
        GetReportProgressQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting report progress for report {ReportId}", request.ReportId);

        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
        {
            logger.LogWarning("User not found for report progress: {UserId}", currentUser.UserId);
            return Errors.Users.UserNotFound;
        }

        var report = await reports.QueryAsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Media)
            .Include(x => x.Assignments)
                .ThenInclude(a => a.AssignedByUser)
            .Include(x => x.Assignments)
                .ThenInclude(a => a.ProgressUpdates)
                    .ThenInclude(u => u.UpdatedByUser)
            .Include(x => x.Assignments)
                .ThenInclude(a => a.ProgressUpdates)
                    .ThenInclude(u => u.Media)
            .Include(x => x.Assignments)
                .ThenInclude(a => a.Team)
                    .ThenInclude(t => t!.Members)
                        .ThenInclude(m => m.User)
            .Include(x => x.StatusHistory)
                .ThenInclude(sh => sh.ChangedByUser)
            .FirstOrDefaultAsync(x => x.Id == request.ReportId, ct)
            .ConfigureAwait(false);

        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        var accessError = ReportReviewCandidateFilters.ValidateLeoReportAccess(
            report, user, currentUser.Role);
        if (accessError is not null)
        {
            logger.LogWarning(
                "User {UserId} denied progress for report {ReportId}: {ErrorCode}",
                currentUser.UserId, request.ReportId, accessError.Code);
            return accessError;
        }

        // ── SLA countdown ──────────────────────────────────────────
        int? hoursRemaining = report.SlaResolveDueAt.HasValue
            ? (int)(report.SlaResolveDueAt.Value - DateTime.UtcNow).TotalHours
            : null;

        var sla = new SlaInfoDto(
            report.SlaResolveDueAt,
            hoursRemaining,
            hoursRemaining.HasValue && hoursRemaining.Value < 0,
            SlaLabels.GetValueOrDefault(report.Severity, report.Severity.ToString()));

        // ── Per-team assignments ───────────────────────────────────
        var assignmentDtos = report.Assignments
            .OrderBy(a => a.AssignedAt)
            .Select(a =>
            {
                var leader = a.Team?.Members.FirstOrDefault(m => m.IsLeader);
                var progressUpdates = MapProgressUpdates(a);
                return new AssignmentProgressDto(
                    a.Id,
                    a.TeamId,
                    a.Team?.Name ?? string.Empty,
                    a.Team?.TeamType.ToString() ?? string.Empty,
                    leader?.User?.FullName,
                    a.AssignedById,
                    a.AssignedByUser?.FullName ?? "Unknown",
                    a.Status.ToString(),
                    a.AssignedAt,
                    a.StartedAt,
                    a.CompletedAt,
                    a.DeclineReason,
                    a.ProgressPercent,
                    a.ProgressNote,
                    a.ProgressUpdatedAt,
                    progressUpdates);
            })
            .ToList();

        // ── Aggregate summary ──────────────────────────────────────
        var allAssignments = report.Assignments.ToList();
        var activeAssignments = allAssignments
            .Where(a => a.Status != AssignmentStatus.Declined)
            .ToList();

        // Completed = 100%, others use their ProgressPercent
        int overallPercent = activeAssignments.Count > 0
            ? (int)activeAssignments.Average(a =>
                a.Status == AssignmentStatus.Completed ? 100 : a.ProgressPercent)
            : 0;

        var summary = new ProgressSummaryDto(
            TotalTeams:              allAssignments.Count,
            AcceptedTeams:           allAssignments.Count(a => a.Status == AssignmentStatus.InProgress),
            CompletedTeams:          allAssignments.Count(a => a.Status == AssignmentStatus.Completed),
            DeclinedTeams:           allAssignments.Count(a => a.Status == AssignmentStatus.Declined),
            PendingTeams:            allAssignments.Count(a => a.Status == AssignmentStatus.Assigned),
            OverallProgressPercent:  overallPercent,
            StartedAt:               report.StartedAt);

        // ── Media grouped by phase ─────────────────────────────────
        var submissionImages = MapMedia(report.Media, MediaType.Image, MediaType.Video);
        var beforeImages = MapMedia(report.Media, MediaType.Before);
        var progressImages = MapMedia(report.Media, MediaType.Progress);
        var afterImages = MapMedia(report.Media, MediaType.After);
        var inspectionImages = MapMedia(report.Media, MediaType.Inspection);
        var reopenEvidenceImages = MapMedia(report.Media, MediaType.ReopenEvidence);

        var media = new ReportMediaGroupDto(
            submissionImages,
            beforeImages,
            progressImages,
            afterImages,
            inspectionImages,
            reopenEvidenceImages);

        var allImages = report.Media
            .Where(m => m.Type != MediaType.Video)
            .OrderBy(m => m.UploadedAt)
            .Select(MapMediaItem)
            .ToList();

        // ── Status history (newest first) ─────────────────────────
        var history = report.StatusHistory
            .OrderByDescending(sh => sh.CreatedAt)
            .Select(sh => new StatusHistoryItemDto(
                sh.FromStatus,
                sh.ToStatus,
                sh.CreatedAt,
                sh.ChangedByUser?.FullName,
                sh.Reason))
            .ToList();

        logger.LogInformation("Progress fetched successfully for report {ReportId}", report.Id);

        return new ReportProgressResponse(
            report.Id,
            report.Code,
            report.Status,
            report.Severity,
            report.Category.NameVi,
            report.Address,
            report.WardCode,
            report.Description,
            sla,
            summary,
            assignmentDtos,
            media,
            allImages,
            history);
    }

    private static List<ProgressUpdateItemDto> MapProgressUpdates(ReportAssignment assignment)
    {
        if (assignment.ProgressUpdates.Count > 0)
        {
            return assignment.ProgressUpdates
                .OrderBy(u => u.CreatedAt)
                .Select(u => new ProgressUpdateItemDto(
                    u.Id,
                    u.ProgressPercent,
                    u.ProgressNote,
                    u.CreatedAt,
                    u.UpdatedByUserId,
                    u.UpdatedByUser?.FullName,
                    u.Media
                        .Where(m => m.Type != MediaType.Video)
                        .OrderBy(m => m.UploadedAt)
                        .Select(MapMediaItem)
                        .ToList()))
                .ToList();
        }

        // Legacy: only latest snapshot on assignment (percent/note); images stay in media.progressImages.
        if (assignment.ProgressUpdatedAt is null && assignment.ProgressPercent == 0)
            return [];

        return
        [
            new ProgressUpdateItemDto(
                assignment.Id,
                assignment.ProgressPercent,
                assignment.ProgressNote,
                assignment.ProgressUpdatedAt ?? assignment.StartedAt ?? assignment.AssignedAt,
                assignment.ProgressUpdatedByUserId ?? assignment.AssignedById,
                null,
                [])
        ];
    }

    private static List<MediaItemDto> MapMedia(
        IEnumerable<ReportMedia> media,
        params MediaType[] types) =>
        media
            .Where(m => types.Contains(m.Type))
            .OrderBy(m => m.UploadedAt)
            .Select(MapMediaItem)
            .ToList();

    private static MediaItemDto MapMediaItem(ReportMedia m) =>
        new(m.Id, m.Type.ToString(), m.Url, m.ThumbnailUrl, m.MimeType, m.SizeBytes, m.UploadedAt);
}
