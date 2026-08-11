using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetCompanyReportDetail;

/// <summary>
/// Returns full detail of a report dispatched to the caller's company:
/// report info, citizen media, SLA, cleanup media (Before/After), team + progress, timeline, waste tags.
/// </summary>
/// <remarks>Implements: BR-CMP-005, BR-CMP-021, BR-CLN-007.</remarks>
public sealed class GetCompanyReportDetailQueryHandler(
    IReportRepository reports,
    IReportMediaRepository reportMedia,
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
        var companyIdResult = await CompanyContextResolver
            .ResolveCompanyIdAsync(companyStaff, currentUser.UserId, ct)
            .ConfigureAwait(false);
        if (companyIdResult.IsFailure)
            return companyIdResult.Error!;

        var companyId = companyIdResult.Value;

        var r = await reports.QueryAsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Media)
            .Include(x => x.VerifiedByUser)
            .Include(x => x.Assignments)
                .ThenInclude(a => a.Team!)
                    .ThenInclude(t => t.Members)
                        .ThenInclude(m => m.User)
            .Include(x => x.Assignments)
                .ThenInclude(a => a.AssignedByUser)
            .Include(x => x.Assignments)
                .ThenInclude(a => a.ProgressUpdates)
                    .ThenInclude(u => u.UpdatedByUser)
            .Include(x => x.Assignments)
                .ThenInclude(a => a.ProgressUpdates)
                    .ThenInclude(u => u.Media)
            .Include(x => x.StatusHistory)
                .ThenInclude(sh => sh.ChangedByUser)
            .Include(x => x.WasteTags)
                .ThenInclude(wt => wt.WasteTag)
            .FirstOrDefaultAsync(x => x.Id == request.ReportId, ct)
            .ConfigureAwait(false);

        if (r is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (r.AssignedCompanyId != companyId)
            return Errors.Reports.ReportNotDispatchedToYourCompany;

        int? hoursRemaining = r.SlaResolveDueAt.HasValue
            ? (int)(r.SlaResolveDueAt.Value - DateTime.UtcNow).TotalHours
            : null;

        var sla = new CompanyReportSlaInfo(
            r.SlaResolveDueAt,
            hoursRemaining,
            hoursRemaining.HasValue && hoursRemaining.Value < 0,
            SlaLabels.GetValueOrDefault(r.Severity, r.Severity.ToString()));

        var citizenMediaByReport = await CitizenReportMediaLoader
            .LoadByReportIdsAsync(reportMedia, [r.Id], ct)
            .ConfigureAwait(false);
        var citizenMedia = CitizenReportMediaLoader.GetMediaOrEmpty(citizenMediaByReport, r.Id);

        var companyAssignments = r.Assignments
            .Where(a => a.Team?.CompanyId == companyId)
            .OrderByDescending(a => a.AssignedAt)
            .ToList();

        var currentAssignment = ResolveCurrentAssignment(companyAssignments);
        CompanyReportTeamAssignment? assignment = currentAssignment is null
            ? null
            : MapAssignment(currentAssignment);

        var assignmentHistory = companyAssignments
            .Select(MapHistoryItem)
            .ToList();

        var canReassign = ComputeCanReassign(r, companyAssignments);

        var beforeImages = r.Media
            .Where(m => m.Type == MediaType.Before)
            .OrderBy(m => m.UploadedAt)
            .Select(MapReportMedia)
            .ToList();

        var afterImages = r.Media
            .Where(m => m.Type == MediaType.After)
            .OrderBy(m => m.UploadedAt)
            .Select(MapReportMedia)
            .ToList();

        var media = new CompanyReportMediaGroup(beforeImages, afterImages);

        var timeline = r.StatusHistory
            .OrderBy(sh => sh.CreatedAt)
            .Select(sh => new CompanyReportTimelineEntry(
                sh.CreatedAt,
                sh.FromStatus,
                sh.ToStatus,
                sh.ChangedByUser?.FullName,
                sh.Reason))
            .ToList();

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
            r.Address, r.WardCode, r.ProvinceCode,
            r.Latitude, r.Longitude,
            r.CreatedAt,
            r.VerifiedAt,
            r.VerifiedByUser?.FullName,
            r.DispatchedToCompanyAt,
            r.ResolvedAt, r.ClosedAt,
            r.ReopenedCount,
            r.PriorityScore,
            sla, citizenMedia, media, assignment, assignmentHistory, canReassign, timeline, wasteTags);
    }

    private static bool ComputeCanReassign(Report report, IReadOnlyList<ReportAssignment> companyAssignments)
    {
        if (report.Status != ReportStatus.InProgress)
            return false;

        if (companyAssignments.Any(a => a.Status == AssignmentStatus.InProgress))
            return false;

        return companyAssignments.Any(a =>
            a.Status is AssignmentStatus.Declined or AssignmentStatus.Assigned);
    }

    private static ReportAssignment? ResolveCurrentAssignment(IEnumerable<ReportAssignment> assignments) =>
        assignments
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefault(a => a.Status != AssignmentStatus.Declined)
        ?? assignments.OrderByDescending(a => a.AssignedAt).FirstOrDefault();

    private static CompanyReportAssignmentHistoryItem MapHistoryItem(ReportAssignment a) =>
        new(
            a.Id,
            a.TeamId,
            a.Team?.Name ?? "Unknown",
            a.Status,
            a.AssignedAt,
            a.StartedAt,
            a.CompletedAt,
            a.DeclineReason,
            a.Note);

    private static CompanyReportTeamAssignment MapAssignment(ReportAssignment a)
    {
        var leader = a.Team?.Members.FirstOrDefault(m => m.IsLeader);

        return new(
            a.Id,
            a.Status,
            a.AssignedAt,
            a.StartedAt,
            a.StartedAt,
            a.CompletedAt,
            a.Note,
            a.DeclineReason,
            a.CheckedInAt,
            a.CheckedInLatitude,
            a.CheckedInLongitude,
            a.CheckedInNote,
            a.ProgressPercent,
            a.ProgressNote,
            a.ProgressUpdatedAt,
            a.ProgressUpdatedByUserId.HasValue
                ? a.Team?.Members.FirstOrDefault(m => m.UserId == a.ProgressUpdatedByUserId)?.User?.FullName
                : null,
            a.TeamId,
            a.Team?.Name ?? "Unknown",
            leader?.User?.FullName,
            a.Team?.Members
                .Select(m => new CompanyReportTeamMember(
                    m.UserId,
                    m.User?.FullName ?? "Unknown",
                    m.User?.AvatarUrl,
                    m.IsLeader,
                    m.JoinedAt))
                .OrderByDescending(m => m.IsLeader)
                .ThenBy(m => m.FullName)
                .ToList() ?? [],
            a.AssignedByUser?.FullName ?? "Unknown",
            MapProgressUpdates(a));
    }

    private static List<CompanyReportProgressUpdateItem> MapProgressUpdates(ReportAssignment assignment)
    {
        if (assignment.ProgressUpdates.Count > 0)
        {
            return assignment.ProgressUpdates
                .OrderBy(u => u.CreatedAt)
                .Select(u => new CompanyReportProgressUpdateItem(
                    u.Id,
                    u.ProgressPercent,
                    u.ProgressNote,
                    u.CreatedAt,
                    u.UpdatedByUserId,
                    u.UpdatedByUser?.FullName,
                    u.Media
                        .Where(m => m.Type != MediaType.Video)
                        .OrderBy(m => m.UploadedAt)
                        .Select(MapReportMedia)
                        .ToList()))
                .ToList();
        }

        if (assignment.ProgressUpdatedAt is null && assignment.ProgressPercent == 0)
            return [];

        return
        [
            new CompanyReportProgressUpdateItem(
                assignment.Id,
                assignment.ProgressPercent,
                assignment.ProgressNote,
                assignment.ProgressUpdatedAt ?? assignment.StartedAt ?? assignment.AssignedAt,
                assignment.ProgressUpdatedByUserId ?? assignment.AssignedById,
                null,
                [])
        ];
    }

    private static CompanyReportMediaItem MapReportMedia(ReportMedia m) =>
        new(m.Id, m.Type.ToString(), m.Url, m.ThumbnailUrl, m.MimeType, m.SizeBytes, m.UploadedAt);
}
