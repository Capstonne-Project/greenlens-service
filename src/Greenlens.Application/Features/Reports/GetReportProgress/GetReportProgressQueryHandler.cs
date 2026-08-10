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
/// Implements: BR-OFF-020 (SLA countdown), BR-OFF-011 (single-team assignment tracking).
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
            .Include(x => x.AssignedCompany)
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
            .Include(x => x.Assignments)
                .ThenInclude(a => a.Team)
                    .ThenInclude(t => t!.Company)
            .Include(x => x.Assignments)
                .ThenInclude(a => a.Team)
                    .ThenInclude(t => t!.LocalOffice)
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
        
        // ── Single team assignment ─────────────────────────────────
        var currentAssignment = ResolveCurrentAssignment(report.Assignments);
        AssignmentProgressDto? assignment = currentAssignment is null
            ? null
            : MapAssignment(currentAssignment);

        AssignedCompanyDto? assignedCompany = null;
        if (report.AssignedCompanyId.HasValue && report.AssignedCompany is not null)
        {
            assignedCompany = new AssignedCompanyDto(
                report.AssignedCompanyId.Value,
                report.AssignedCompany.Name,
                report.DispatchedToCompanyAt);
        }

        // ── Media grouped by phase ─────────────────────────────────
        var submissionImages = MapMedia(report.Media, MediaType.Image, MediaType.Video);
        var beforeImages = MapMedia(report.Media, MediaType.Before);
        var afterImages = MapMedia(report.Media, MediaType.After);
        var inspectionImages = MapMedia(report.Media, MediaType.Inspection);
        var reopenEvidenceImages = MapMedia(report.Media, MediaType.ReopenEvidence);

        var media = new ReportMediaGroupDto(
            submissionImages,
            beforeImages,
            afterImages,
            inspectionImages,
            reopenEvidenceImages);

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
            assignedCompany,
            assignment,
            media,
            history);
    }

    private static ReportAssignment? ResolveCurrentAssignment(IEnumerable<ReportAssignment> assignments) =>
        assignments
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefault(a => a.Status != AssignmentStatus.Declined)
        ?? assignments.OrderByDescending(a => a.AssignedAt).FirstOrDefault();

    private static AssignmentProgressDto MapAssignment(ReportAssignment a)
    {
        var team = a.Team;
        var leader = team?.Members.FirstOrDefault(m => m.IsLeader);
        var members = MapTeamMembers(team);

        return new AssignmentProgressDto(
            a.Id,
            a.TeamId,
            team?.Name ?? string.Empty,
            team?.TeamType.ToString() ?? string.Empty,
            team?.IsCompanyTeam ?? false,
            team?.CompanyId,
            team?.Company?.Name,
            team?.LocalOfficeId,
            team?.LocalOffice?.Name,
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
            members,
            MapProgressUpdates(a));
    }

    private static List<AssignmentTeamMemberDto> MapTeamMembers(EnvironmentalTeam? team) =>
        team?.Members
            .OrderByDescending(m => m.IsLeader)
            .ThenBy(m => m.JoinedAt)
            .Select(m => new AssignmentTeamMemberDto(
                m.UserId,
                m.User?.FullName,
                m.User?.Email,
                m.User?.PhoneNumber,
                m.User?.AvatarUrl,
                m.IsLeader,
                m.JoinedAt))
            .ToList() ?? [];

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

        // Legacy: only latest snapshot on assignment (percent/note); images in progressUpdates when present.
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
