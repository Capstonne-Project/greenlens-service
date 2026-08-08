using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.ResolveReport;

/// <summary>
/// Cleanup Team marks their assignment as completed. BR-CLN-005: ≥ 2 after images.
/// When ALL assignments are completed → report transitions to Resolved.
/// </summary>
public sealed class ResolveReportCommandHandler(
    IReportRepository reports,
    IReportAssignmentRepository assignments,
    ITeamMemberRepository teamMembers,
    IReportStatusHistoryRepository statusHistory,
    IReportMediaRepository reportMedia,
    IFileStorageService fileStorage,
    ICleanupAssignmentActivityNotifier activityNotifier,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<ResolveReportCommandHandler> logger) : IRequestHandler<ResolveReportCommand, Result>
{
    public async Task<Result> Handle(ResolveReportCommand request, CancellationToken ct)
    {
        logger.LogInformation("Resolving report {ReportId}", request.ReportId);

        // BR-CLN-005: at least 2 after images
        if (request.AfterImageUrls.Count < 2)
        {
            logger.LogWarning("Insufficient after images for report {ReportId}", request.ReportId);
            return Errors.Reports.InsufficientAfterImages;
        }

        foreach (var url in request.AfterImageUrls)
        {
            if (!fileStorage.IsOwnedPublicUrl(url))
            {
                logger.LogWarning("Invalid storage URL for after image {Url}", url);
                return Errors.Media.InvalidStorageUrl;
            }
        }

        var leader = await teamMembers.GetLeaderByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (leader is null)
        {
            logger.LogWarning("Team leader not found for user {UserId}", currentUser.UserId);
            return Errors.Reports.NotTeamLeader;
        }

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (report.Status != ReportStatus.InProgress)
        {
            logger.LogWarning("Report {ReportId} is not in a valid status for resolution", request.ReportId);
            return Errors.Reports.InvalidStatusTransition;
        }

        // BR-REP-014: Must have ≥ 1 before image uploaded during check-in
        var hasBeforeImage = await reportMedia.QueryAsNoTracking()
            .AnyAsync(m => m.ReportId == request.ReportId && m.Type == MediaType.Before, ct)
            .ConfigureAwait(false);
        if (!hasBeforeImage)
        {
            logger.LogWarning("Missing before image for report {ReportId}", request.ReportId);
            return Errors.Reports.MissingBeforeImages;
        }

        // Find this team's assignment via token — no need to pass teamId in body
        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var assignment = reportAssignments.FirstOrDefault(a => a.TeamId == leader.TeamId);
        if (assignment is null)
        {
            logger.LogWarning("Assignment not found for report {ReportId} and team {TeamId}", request.ReportId, leader.TeamId);
            return Errors.Reports.AssignmentNotFound;
        }

        if (assignment.Status != AssignmentStatus.InProgress)
        {
            logger.LogWarning("Assignment {AssignmentId} is not in a valid status for resolution", assignment.Id);
            return Errors.Reports.InvalidStatusTransition;
        }

        assignment.Complete();

        // Persist after images as ReportMedia (Type = After) for LEO visibility
        foreach (var url in request.AfterImageUrls)
        {
            var media = ReportMedia.Create(
                request.ReportId,
                MediaType.After,
                url,
                "image/jpeg",
                0L,
                currentUser.UserId);
            reportMedia.Add(media);
        }

        // Check if ALL active assignments (non-declined) are completed
        var activeAssignments = reportAssignments
            .Where(a => a.Status != AssignmentStatus.Declined)
            .ToList();

        var allCompleted = activeAssignments.All(a => a.Status == AssignmentStatus.Completed);

        if (allCompleted)
        {
            // All teams done → transition report to Resolved
            report.Resolve();

            var history = ReportStatusHistory.Create(
                report.Id,
                ReportStatus.InProgress,
                ReportStatus.Resolved,
                currentUser.UserId);

            statusHistory.Add(history);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await activityNotifier.NotifyCompletedAsync(
            assignment.AssignedById,
            leader.TeamId,
            report.Id,
            report.Code,
            allCompleted,
            ct).ConfigureAwait(false);

        if (allCompleted)
            logger.LogInformation("Report {ReportId} resolved — all teams completed", report.Id);
        else
            logger.LogInformation("Team {TeamId} completed assignment for report {ReportId}",
                leader.TeamId, report.Id);

        return Result.Success();
    }
}
