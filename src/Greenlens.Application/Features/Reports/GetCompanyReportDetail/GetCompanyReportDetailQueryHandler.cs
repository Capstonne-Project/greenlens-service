using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetCompanyReportDetail;

/// <summary>
/// Returns full detail of a report dispatched to the caller's company:
/// report info, SLA, progress summary, media (Before/Progress/After),
/// all team assignments + members + progress, status timeline, waste tags.
/// </summary>
public sealed class GetCompanyReportDetailQueryHandler(
    IReportRepository reports,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    ILogger<GetCompanyReportDetailQueryHandler> logger)
    : IRequestHandler<GetCompanyReportDetailQuery, Result<CompanyReportDetailResponse>>
{
    private static readonly Dictionary<Severity, string> SlaLabels = new()
    {
        { Severity.Critical, "Critical (3 ngày)" },
        { Severity.High,     "High (5 ngày)" },
        { Severity.Medium,   "Medium (7 ngày)" },
        { Severity.Low,      "Low (10 ngày)" },
    };

    public async Task<Result<CompanyReportDetailResponse>> Handle(
        GetCompanyReportDetailQuery request, CancellationToken ct)
    {
        // ── 1. Resolve caller's company ──
        var staff = await companyStaff.GetByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (staff is null || !staff.IsActive)
            return Errors.Reports.ReportNotDispatchedToYourCompany;

        var companyId = staff.CompanyId;

        // ── 2. Load report with all related data ──
        var r = await reports.QueryAsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Media)
            .Include(x => x.Assignments).ThenInclude(a => a.Team!).ThenInclude(t => t.Members).ThenInclude(m => m.User)
            .Include(x => x.Assignments).ThenInclude(a => a.AssignedByUser)
            .Include(x => x.StatusHistory).ThenInclude(sh => sh.ChangedByUser)
            .Include(x => x.WasteTags).ThenInclude(wt => wt.WasteTag)
            .FirstOrDefaultAsync(x => x.Id == request.ReportId, ct)
            .ConfigureAwait(false);

        if (r is null)
            return Errors.Reports.ReportNotFound;

        // ── 3. Verify report belongs to this company ──
        if (r.AssignedCompanyId != companyId)
            return Errors.Reports.ReportNotDispatchedToYourCompany;

        // ── 4. SLA countdown ──
        int? hoursRemaining = r.SlaResolveDueAt.HasValue
            ? (int)(r.SlaResolveDueAt.Value - DateTime.UtcNow).TotalHours
            : null;

        var sla = new CompanyReportSlaInfo(
            r.SlaResolveDueAt,
            hoursRemaining,
            hoursRemaining.HasValue && hoursRemaining.Value < 0,
            SlaLabels.GetValueOrDefault(r.Severity, r.Severity.ToString()));

        // ── 5. Company team assignments ──
        var companyAssignments = r.Assignments
            .Where(a => a.Team?.CompanyId == companyId)
            .ToList();

        var teamAssignments = companyAssignments
            .OrderByDescending(a => a.AssignedAt)
            .Select(a => new CompanyReportTeamAssignment(
                a.Id,
                a.Status,
                a.AssignedAt,
                a.StartedAt,
                a.CompletedAt,
                a.Note,
                a.DeclineReason,
                a.ProgressPercent,
                a.ProgressNote,
                a.ProgressUpdatedAt,
                a.ProgressUpdatedByUserId.HasValue
                    ? a.Team?.Members.FirstOrDefault(m => m.UserId == a.ProgressUpdatedByUserId)?.User?.FullName
                    : null,
                a.TeamId,
                a.Team?.Name ?? "Unknown",
                a.Team?.Members
                    .Select(m => new CompanyReportTeamMember(
                        m.UserId,
                        m.User?.FullName ?? "Unknown",
                        m.IsLeader))
                    .OrderByDescending(m => m.IsLeader)
                    .ToList() ?? [],
                a.AssignedByUser?.FullName ?? "Unknown"))
            .ToList();

        // ── 6. Progress summary (company teams only) ──
        var activeAssignments = companyAssignments
            .Where(a => a.Status != AssignmentStatus.Declined)
            .ToList();

        int overallPercent = activeAssignments.Count > 0
            ? (int)activeAssignments.Average(a =>
                a.Status == AssignmentStatus.Completed ? 100 : a.ProgressPercent)
            : 0;

        var summary = new CompanyReportProgressSummary(
            TotalTeams:             companyAssignments.Count,
            AcceptedTeams:          companyAssignments.Count(a => a.Status == AssignmentStatus.InProgress),
            CompletedTeams:         companyAssignments.Count(a => a.Status == AssignmentStatus.Completed),
            DeclinedTeams:          companyAssignments.Count(a => a.Status == AssignmentStatus.Declined),
            PendingTeams:           companyAssignments.Count(a => a.Status == AssignmentStatus.Assigned),
            OverallProgressPercent: overallPercent,
            StartedAt:             r.StartedAt);

        // ── 7. Media grouped by phase ──
        var beforeImages = r.Media
            .Where(m => m.Type is MediaType.Before or MediaType.Image)
            .OrderBy(m => m.UploadedAt)
            .Select(m => new CompanyReportMediaItem(m.Url, m.UploadedAt))
            .ToList();

        var progressImages = r.Media
            .Where(m => m.Type == MediaType.Progress)
            .OrderBy(m => m.UploadedAt)
            .Select(m => new CompanyReportMediaItem(m.Url, m.UploadedAt))
            .ToList();

        var afterImages = r.Media
            .Where(m => m.Type == MediaType.After)
            .OrderBy(m => m.UploadedAt)
            .Select(m => new CompanyReportMediaItem(m.Url, m.UploadedAt))
            .ToList();

        var media = new CompanyReportMediaGroup(beforeImages, progressImages, afterImages);

        // ── 8. Status timeline (oldest → newest) ──
        var timeline = r.StatusHistory
            .OrderBy(sh => sh.CreatedAt)
            .Select(sh => new CompanyReportTimelineEntry(
                sh.CreatedAt,
                sh.FromStatus,
                sh.ToStatus,
                sh.ChangedByUser?.FullName,
                sh.Reason))
            .ToList();

        // ── 9. Waste tags ──
        var wasteTags = r.WasteTags
            .Where(wt => wt.WasteTag is not null)
            .Select(wt => new CompanyReportWasteTag(
                wt.WasteTagId,
                wt.WasteTag!.Code,
                wt.WasteTag.NameVi,
                wt.WasteTag.IconUrl))
            .ToList();

        logger.LogInformation(
            "CM {UserId} viewed report detail {ReportId} (company {CompanyId})",
            currentUser.UserId, r.Id, companyId);

        return new CompanyReportDetailResponse(
            r.Id, r.Code, r.Status, r.Severity,
            r.Category.NameVi, r.Description,
            r.Address, r.WardCode, r.Latitude, r.Longitude,
            r.CreatedAt, r.DispatchedToCompanyAt,
            r.ResolvedAt, r.ClosedAt,
            r.ReopenedCount,
            sla, summary, media,
            teamAssignments, timeline, wasteTags);
    }
}
