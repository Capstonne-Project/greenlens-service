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

namespace Greenlens.Application.Features.Reports.UpdateCleanupProgress;

/// <summary>
/// BR-CLN-004: Update cleanup progress. Must be InProgress.
/// </summary>
public sealed class UpdateCleanupProgressCommandHandler(
    IReportRepository reports,
    IReportAssignmentRepository assignments,
    IAssignmentProgressUpdateRepository progressUpdates,
    IGeoDistanceService geoDistance,
    ISystemSettingsProvider systemSettings,
    ICleanupAssignmentActivityNotifier activityNotifier,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<UpdateCleanupProgressCommandHandler> logger)
    : IRequestHandler<UpdateCleanupProgressCommand, Result>
{
    public async Task<Result> Handle(UpdateCleanupProgressCommand request, CancellationToken ct)
    {
        var maxProgressDistanceMeters = ModuleSystemSettings.ProgressUpdateMaxDistanceMeters(systemSettings);

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (report.Status != ReportStatus.InProgress)
        {
            logger.LogWarning("Report {ReportId} is not in a valid status for cleanup progress update", request.ReportId);
            return Errors.Reports.InvalidStatusTransition;
        }

        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var assignment = ReportAssignmentSelection.SelectLatestForTeam(reportAssignments, request.TeamId);

        if (assignment is null)
        {
            logger.LogWarning("Assignment not found for report {ReportId} and team {TeamId}", request.ReportId, request.TeamId);
            return Errors.Reports.AssignmentNotFound;
        }
        if (assignment.Status != AssignmentStatus.InProgress)
        {
            logger.LogWarning("Assignment {AssignmentId} is not in a valid status for cleanup progress update", assignment.Id);
            return Errors.Cleanup.AssignmentNotInProgress;
        }

        if (request.Percent < assignment.ProgressPercent)
        {
            logger.LogWarning("Progress {Percent}% is lower than current {CurrentPercent}% for assignment {AssignmentId}",
                request.Percent, assignment.ProgressPercent, assignment.Id);
            return Errors.Cleanup.ProgressCannotDecrease(assignment.ProgressPercent);
        }

        // BR-CLN-004: progress updates must be submitted from near the site.
        var distance = await geoDistance.GetDistanceInMetersAsync(
            request.Latitude, request.Longitude,
            report.Latitude, report.Longitude, ct).ConfigureAwait(false);

        if (distance > maxProgressDistanceMeters)
        {
            logger.LogWarning("Distance {Distance} is greater than {MaxProgressDistanceMeters} for report {ReportId}",
                distance, maxProgressDistanceMeters, request.ReportId);
            return Errors.Progress.TooFarFromSite(distance);
        }

        progressUpdates.Add(AssignmentProgressUpdate.Create(
            assignment.Id,
            request.ReportId,
            request.Percent,
            request.Note,
            currentUser.UserId));

        assignment.UpdateProgress(request.Percent, request.Note, currentUser.UserId);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await activityNotifier.NotifyProgressUpdatedAsync(
            assignment.AssignedById,
            request.TeamId,
            report.Id,
            report.Code,
            request.Percent,
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Team {TeamId} updated progress for report {ReportId}: {Percent}%",
            request.TeamId, report.Id, request.Percent);

        return Result.Success();
    }
}
