using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.DeclineAssignment;

/// <summary>
/// Team declines within 24h window. BR-CLN-007, BR-INS-003.
/// Report stays InProgress; LEO reassigns via PUT /reports/{id}/reassign after reviewing progress.
/// </summary>
public sealed class DeclineAssignmentCommandHandler(
    IReportRepository reports,
    IReportAssignmentRepository assignments,
    ICleanupAssignmentActivityNotifier activityNotifier,
    IUnitOfWork uow,
    ILogger<DeclineAssignmentCommandHandler> logger) : IRequestHandler<DeclineAssignmentCommand, Result>
{
    public async Task<Result> Handle(DeclineAssignmentCommand request, CancellationToken ct)
    {
        logger.LogInformation("Declining assignment for report {ReportId}", request.ReportId);

        if (request.Reason.Length < 20)
        {
            logger.LogWarning("Reason is too short for report {ReportId}", request.ReportId);
            return Errors.Reports.ReasonTooShort;
        }

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (report.Status != ReportStatus.InProgress)
        {
            logger.LogWarning("Report {ReportId} is not in progress", request.ReportId);
            return Errors.Reports.InvalidStatusTransition;
        }

        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var assignment = ReportAssignmentSelection.SelectLatestForTeam(reportAssignments, request.TeamId);

        if (assignment is null)
        {
            logger.LogWarning("Assignment not found for report ID {ReportId} and team ID {TeamId}", request.ReportId, request.TeamId);
            return Errors.Reports.AssignmentNotFound;
        }

        if (assignment.Status != AssignmentStatus.Assigned)
        {
            logger.LogWarning("Assignment {AssignmentId} is not assigned", assignment.Id);
            return Errors.Reports.InvalidStatusTransition;
        }

        // BR-CLN-007, BR-INS-003: 24h window (updated from 2h per business decision)
        if ((DateTime.UtcNow - assignment.AssignedAt).TotalHours > 24)
        {
            logger.LogWarning("Decline window expired for assignment {AssignmentId}", assignment.Id);
            return Errors.Reports.DeclineWindowExpired;
        }

        assignment.Decline(request.Reason);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await activityNotifier.NotifyDeclinedAsync(
            assignment.AssignedById,
            request.TeamId,
            report.Id,
            report.Code,
            request.Reason,
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Team {TeamId} declined assignment for report {ReportId}; report remains InProgress",
            request.TeamId, report.Id);

        return Result.Success();
    }
}
