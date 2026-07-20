using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
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
        // Any member of the team can view (not just leader)
        var membership = await teamMembers
            .QueryAsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == currentUser.UserId, ct)
            .ConfigureAwait(false);

        if (membership is null)
            return Errors.Reports.NotTeamMember;

        var assignment = await assignments
            .QueryAsNoTracking()
            .Include(a => a.Report)
                .ThenInclude(r => r!.Category)
            .Include(a => a.Report)
                .ThenInclude(r => r!.Media)
            .Include(a => a.Report)
                .ThenInclude(r => r!.WasteTags)
                    .ThenInclude(wt => wt.WasteTag)
            .FirstOrDefaultAsync(
                a => a.ReportId == request.ReportId && a.TeamId == membership.TeamId, ct)
            .ConfigureAwait(false);

        if (assignment is null)
            return Errors.Reports.AssignmentNotFound;

        var report = assignment.Report!;

        var now = DateTime.UtcNow;
        const int declineWindowHours = 24;
        const int progressUpdateIntervalHours = 24;

        var declineDeadlineAt = assignment.AssignedAt.AddHours(declineWindowHours);
        var canDecline = assignment.Status == AssignmentStatus.Assigned
            && now <= declineDeadlineAt;

        var beforeImageCount = report.Media.Count(m => m.Type == MediaType.Before);
        var hasBeforeImages = beforeImageCount > 0;

        var canUpdateProgress = assignment.Status == AssignmentStatus.InProgress;
        // Resolve requires before images (BR-REP-014) — surface that in the flag for FE
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
            ProgressNote: assignment.ProgressNote,
            ProgressUpdatedAt: assignment.ProgressUpdatedAt,
            ProgressUpdatedByUserId: assignment.ProgressUpdatedByUserId,

            AssignmentNote: assignment.Note,

            WasteTags: wasteTagItems,

            DeclineDeadlineAt: declineDeadlineAt,
            HasBeforeImages: hasBeforeImages,
            BeforeImageCount: beforeImageCount,
            ProgressRequiredByAt: progressRequiredByAt
        );
    }
}
