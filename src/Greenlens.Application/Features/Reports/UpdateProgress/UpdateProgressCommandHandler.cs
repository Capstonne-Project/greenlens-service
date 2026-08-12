using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.UpdateProgress;

/// <summary>
/// Team leader updates cleanup progress (percent, note, optional R2 image URLs).
/// TeamId resolved from JWT token — caller must be a team leader.
/// Does NOT change report or assignment status. Each update is stored as history.
/// </summary>
/// <remarks>Implements: BR-CLN-004 (progress tracking).</remarks>
public sealed class UpdateProgressCommandHandler(
    IReportRepository reports,
    IReportAssignmentRepository assignments,
    IAssignmentProgressUpdateRepository progressUpdates,
    ITeamMemberRepository teamMembers,
    IReportMediaRepository reportMedia,
    IFileStorageService fileStorage,
    ICleanupAssignmentActivityNotifier activityNotifier,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<UpdateProgressCommandHandler> logger) : IRequestHandler<UpdateProgressCommand, Result<UpdateProgressResponse>>
{
    private const int MaxImages = 5;

    public async Task<Result<UpdateProgressResponse>> Handle(UpdateProgressCommand request, CancellationToken ct)
    {
        logger.LogInformation("Updating progress for report {ReportId} with {ProgressPercent}% and {ImageUrls} for user {UserId}",
            request.ReportId, request.ProgressPercent, request.ImageUrls, currentUser.UserId);
        
        if (request.ProgressPercent is < 0 or > 100)
        {
            logger.LogWarning("Invalid progress percent for report {ReportId}", request.ReportId);
            return Errors.Reports.InvalidProgressPercent;
        }

        if (request.ImageUrls.Count > MaxImages)
        {
            logger.LogWarning("Too many images for report {ReportId}", request.ReportId);
            return Errors.Media.TooManyImages;
        }

        foreach (var url in request.ImageUrls)
        {
            if (!fileStorage.IsOwnedPublicUrl(url))
            {
                logger.LogWarning("Invalid storage URL for image {Url}", url);
                return Errors.Media.InvalidStorageUrl;
            }
        }

        var leader = await teamMembers.GetLeaderByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (leader is null)
        {
            logger.LogWarning("Team leader not found for user {UserId}", currentUser.UserId);
            return Errors.Reports.NotTeamLeader;
        }

        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var assignment = ReportAssignmentSelection.SelectLatestForTeam(reportAssignments, leader.TeamId);

        if (assignment is null)
        {
            logger.LogWarning("Assignment not found for report {ReportId} and team {TeamId}", request.ReportId, leader.TeamId);
            return Errors.Reports.AssignmentNotFound;
        }

        if (assignment.Status != AssignmentStatus.InProgress)
        {
            logger.LogWarning("Assignment {AssignmentId} is not in a valid status for progress update", assignment.Id);
            return Errors.Reports.AssignmentNotInProgress;
        }

        if (request.ProgressPercent < assignment.ProgressPercent)
        {
            logger.LogWarning("Progress {Percent}% is lower than current {CurrentPercent}% for assignment {AssignmentId}",
                request.ProgressPercent, assignment.ProgressPercent, assignment.Id);
            return Errors.Reports.ProgressCannotDecrease(assignment.ProgressPercent);
        }

        var progressUpdate = AssignmentProgressUpdate.Create(
            assignment.Id,
            request.ReportId,
            request.ProgressPercent,
            request.ProgressNote,
            currentUser.UserId);
        progressUpdates.Add(progressUpdate);

        var savedUrls = new List<string>(request.ImageUrls.Count);
        foreach (var url in request.ImageUrls)
        {
            var trimmed = url.Trim();
            var media = ReportMedia.Create(
                request.ReportId,
                MediaType.Progress,
                trimmed,
                "image/jpeg",
                0L,
                currentUser.UserId,
                progressUpdateId: progressUpdate.Id);
            reportMedia.Add(media);
            savedUrls.Add(trimmed);
        }

        assignment.UpdateProgress(request.ProgressPercent, request.ProgressNote, currentUser.UserId);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is not null)
        {
            await activityNotifier.NotifyProgressUpdatedAsync(
                assignment.AssignedById,
                leader.TeamId,
                report.Id,
                report.Code,
                request.ProgressPercent,
                ct).ConfigureAwait(false);
        }

        logger.LogInformation("Progress updated to {Percent}% for report {ReportId} by team {TeamId}",
            request.ProgressPercent, request.ReportId, leader.TeamId);

        return new UpdateProgressResponse(savedUrls);
    }
}
