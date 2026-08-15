using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.EscalateCleanup;

/// <summary>
/// BR-CLN-006: Team escalates to LEO — beyond their capability.
/// Assignment → Escalated, report → Verified for LEO re-assignment.
/// </summary>
public sealed class EscalateCleanupCommandHandler(
    IReportRepository reports,
    IReportAssignmentRepository assignments,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<EscalateCleanupCommandHandler> logger)
    : IRequestHandler<EscalateCleanupCommand, Result>
{
    public async Task<Result> Handle(EscalateCleanupCommand request, CancellationToken ct)
    {
        logger.LogInformation("Escalating cleanup for report {ReportId}", request.ReportId);

        if (request.Reason.Length < 20)
        {
            logger.LogWarning("Reason is too short for report {ReportId}", request.ReportId);
            return Errors.Cleanup.EscalateReasonRequired;
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

        if (assignment.Status != AssignmentStatus.InProgress)
        {
            logger.LogWarning("Assignment {AssignmentId} is not in progress", assignment.Id);
            return Errors.Cleanup.AssignmentNotInProgress;
        }

        // Escalate this assignment
        assignment.Escalate(request.Reason);

        // Current-cycle teams only — ignore prior-cycle Completed rows (BR-REP-015)
        if (ReportAssignmentSelection.AllCurrentCycleEscalatedOrCompleted(
                reportAssignments,
                report.Status,
                report.ReopenedCount,
                report.StatusHistory))
        {
            logger.LogWarning("All active assignments are now escalated/declined → reverting report {ReportId} to Verified", request.ReportId);
            report.ForceStatus(ReportStatus.Verified);

            var history = ReportStatusHistory.Create(
                report.Id,
                ReportStatus.InProgress,
                ReportStatus.Verified,
                currentUser.UserId,
                reason: $"Escalated: {request.Reason}");

            statusHistory.Add(history);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogWarning(
            "Team {TeamId} escalated report {ReportId}: {Reason}",
            request.TeamId, report.Id, request.Reason);

        return Result.Success();
    }
}
