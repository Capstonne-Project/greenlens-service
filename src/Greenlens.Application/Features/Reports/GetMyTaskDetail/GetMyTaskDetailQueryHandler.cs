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

namespace Greenlens.Application.Features.Reports.GetMyTaskDetail;

/// <summary>
/// Returns full task detail from the perspective of the current user's team.
/// Any team member (not just leader) can view.
/// </summary>
public sealed class GetMyTaskDetailQueryHandler(
    IReportAssignmentRepository assignments,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    ILogger<GetMyTaskDetailQueryHandler> logger)
    : IRequestHandler<GetMyTaskDetailQuery, Result<MyTaskDetailResponse>>
{
    public async Task<Result<MyTaskDetailResponse>> Handle(
        GetMyTaskDetailQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting my task detail for user {UserId}", currentUser.UserId);

        // Any member of any team the user belongs to can view (not just leader)
        var myTeamIds = await teamMembers
            .QueryAsNoTracking()
            .Where(m => m.UserId == currentUser.UserId)
            .Select(m => m.TeamId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (myTeamIds.Count == 0)
        {
            logger.LogWarning("Team member not found for user {UserId}", currentUser.UserId);
            return Errors.Reports.NotTeamMember;
        }

        var assignmentRows = await assignments
            .QueryAsNoTracking()
            .Include(a => a.Report)
                .ThenInclude(r => r!.Category)
            .Include(a => a.Report)
                .ThenInclude(r => r!.Media)
            .Include(a => a.Report)
                .ThenInclude(r => r!.WasteTags)
                    .ThenInclude(wt => wt.WasteTag)
            .Include(a => a.ProgressUpdates)
                .ThenInclude(u => u.Media)
            .Where(a => a.ReportId == request.ReportId && myTeamIds.Contains(a.TeamId))
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var assignment = ReportAssignmentSelection.SelectLatestForTeams(assignmentRows, myTeamIds);

        if (assignment is null)
        {
            logger.LogWarning(
                "Assignment not found for report ID {ReportId} and user teams {TeamIds}",
                request.ReportId,
                string.Join(',', myTeamIds));
            return Errors.Reports.AssignmentNotFound;
        }

        var report = assignment.Report!;

        var now = DateTime.UtcNow;
        const int declineWindowHours = 24;
        const int progressUpdateIntervalHours = 24;

        var declineDeadlineAt = assignment.AssignedAt.AddHours(declineWindowHours);
        var canDecline = assignment.Status == AssignmentStatus.Assigned
            && now <= declineDeadlineAt;

        var beforeMedia = ReportAssignmentMediaScope.FilterForAssignment(
            report.Media, assignment, MediaType.Before);
        var afterMedia = ReportAssignmentMediaScope.FilterForAssignment(
            report.Media, assignment, MediaType.After);

        var beforeImageCount = beforeMedia.Count;
        var hasBeforeImages = beforeImageCount > 0;

        var canUpdateProgress = assignment.Status == AssignmentStatus.InProgress;
        var canResolve = assignment.Status == AssignmentStatus.InProgress && hasBeforeImages;

        DateTime? progressRequiredByAt = null;
        if (assignment.Status == AssignmentStatus.InProgress)
        {
            var progressAnchor = assignment.ProgressUpdatedAt ?? assignment.StartedAt ?? assignment.AssignedAt;
            progressRequiredByAt = progressAnchor.AddHours(progressUpdateIntervalHours);
        }

        var images = report.Media
            .Where(m => m.Type == MediaType.Image)
            .OrderBy(m => m.UploadedAt)
            .Select(m => new TaskImageItem(m.Url, m.MimeType))
            .ToList();

        var beforeImages = beforeMedia
            .Select(m => new TaskImageItem(m.Url, m.MimeType))
            .ToList();

        var afterImages = afterMedia
            .Select(m => new TaskImageItem(m.Url, m.MimeType))
            .ToList();

        var progressUpdates = MapProgressUpdates(assignment);

        var latestProgressNote = ReportAssignmentMediaScope.ResolveLatestProgressNote(assignment);

        var wasteTagItems = report.WasteTags
            .Where(wt => wt.WasteTag is not null)
            .Select(wt => new TaskWasteTagItem(
                wt.WasteTag!.Code, wt.WasteTag.NameVi,
                wt.WasteTag.NameEn, wt.WasteTag.IconUrl))
            .ToList();

        logger.LogInformation("Lấy chi tiết báo cáo thành công. Mã báo cáo: {ReportCode}", report.Code);
        return new MyTaskDetailResponse(
            AssignmentId: assignment.Id,
            AssignmentStatus: assignment.Status,
            AssignedAt: assignment.AssignedAt,
            StartedAt: assignment.StartedAt,
            CompletedAt: assignment.CompletedAt,
            CanDecline: canDecline,
            CanUpdateProgress: canUpdateProgress,
            CanResolve: canResolve,

            ReportId: report.Id,
            ReportCode: report.Code,
            ReportStatus: report.Status,
            CategoryCode: report.Category!.Code,
            CategoryName: report.Category.NameVi,
            Severity: report.Severity,
            Description: report.Description,
            Latitude: report.Latitude,
            Longitude: report.Longitude,
            Address: report.Address,
            WardCode: report.WardCode,

            SlaResolveDueAt: report.SlaResolveDueAt,

            ReportImages: images,

            ProgressPercent: assignment.ProgressPercent,
            ProgressNote: latestProgressNote,
            ProgressUpdatedAt: assignment.ProgressUpdatedAt,
            ProgressUpdatedByUserId: assignment.ProgressUpdatedByUserId,

            AssignmentNote: assignment.Note,

            WasteTags: wasteTagItems,

            DeclineDeadlineAt: declineDeadlineAt,
            HasBeforeImages: hasBeforeImages,
            BeforeImageCount: beforeImageCount,
            BeforeImages: beforeImages,
            AfterImages: afterImages,
            ProgressUpdates: progressUpdates,
            ProgressRequiredByAt: progressRequiredByAt
        );
    }

    private static List<TaskProgressUpdateItem> MapProgressUpdates(ReportAssignment assignment)
    {
        if (assignment.ProgressUpdates.Count > 0)
        {
            return assignment.ProgressUpdates
                .OrderBy(u => u.CreatedAt)
                .Select(u => new TaskProgressUpdateItem(
                    u.Id,
                    u.ProgressPercent,
                    u.ProgressNote,
                    u.CreatedAt,
                    u.Media
                        .Where(m => m.Type != MediaType.Video)
                        .OrderBy(m => m.UploadedAt)
                        .Select(m => new TaskImageItem(m.Url, m.MimeType))
                        .ToList()))
                .ToList();
        }

        if (assignment.ProgressUpdatedAt is null && assignment.ProgressPercent == 0)
            return [];

        return
        [
            new TaskProgressUpdateItem(
                assignment.Id,
                assignment.ProgressPercent,
                assignment.ProgressNote,
                assignment.ProgressUpdatedAt ?? assignment.StartedAt ?? assignment.AssignedAt,
                [])
        ];
    }
}
