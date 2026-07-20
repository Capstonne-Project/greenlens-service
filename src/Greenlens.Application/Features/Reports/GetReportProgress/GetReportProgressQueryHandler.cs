using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
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
/// </remarks>
public sealed class GetReportProgressQueryHandler(
    IReportRepository reports,
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
        var report = await reports.QueryAsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Media)
            .Include(x => x.Assignments)
                .ThenInclude(a => a.Team)
                    .ThenInclude(t => t!.Members)
                        .ThenInclude(m => m.User)
            .Include(x => x.StatusHistory)
                .ThenInclude(sh => sh.ChangedByUser)
            .FirstOrDefaultAsync(x => x.Id == request.ReportId, ct)
            .ConfigureAwait(false);

        if (report is null)
            return Errors.Reports.ReportNotFound;

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
                return new AssignmentProgressDto(
                    a.Id,
                    a.TeamId,
                    a.Team?.Name ?? string.Empty,
                    a.Team?.TeamType.ToString() ?? string.Empty,
                    leader?.User?.FullName,
                    a.Status.ToString(),
                    a.AssignedAt,
                    a.StartedAt,
                    a.CompletedAt,
                    a.DeclineReason,
                    a.ProgressPercent,
                    a.ProgressNote,
                    a.ProgressUpdatedAt);
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
        var beforeImages = report.Media
            .Where(m => m.Type is MediaType.Before or MediaType.Image)
            .OrderBy(m => m.UploadedAt)
            .Select(m => new MediaItemDto(m.Url, m.UploadedAt))
            .ToList();

        var progressImages = report.Media
            .Where(m => m.Type == MediaType.Progress)
            .OrderBy(m => m.UploadedAt)
            .Select(m => new MediaItemDto(m.Url, m.UploadedAt))
            .ToList();

        var afterImages = report.Media
            .Where(m => m.Type == MediaType.After)
            .OrderBy(m => m.UploadedAt)
            .Select(m => new MediaItemDto(m.Url, m.UploadedAt))
            .ToList();

        var media = new ReportMediaGroupDto(beforeImages, progressImages, afterImages);

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

        logger.LogInformation("LEO fetched progress for report {ReportId}", report.Id);

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
            history);
    }
}
