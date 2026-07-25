using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.DeclineAssignment;

/// <summary>
/// Team declines within 2h window. BR-CLN-007, BR-INS-003.
/// If ALL assignments are declined → report reverts to Verified so LEO can re-assign.
/// </summary>
public sealed class DeclineAssignmentCommandHandler(
    IReportRepository reports,
    IReportAssignmentRepository assignments,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
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
        var assignment = reportAssignments.FirstOrDefault(a => a.TeamId == request.TeamId);

        if (assignment is null)
        {
            logger.LogWarning("Assignment not found for report ID {ReportId} and team ID {TeamId}", request.ReportId, request.TeamId);
            return Errors.Reports.AssignmentNotFound;
        }

        // DC1: must be Assigned (not yet accepted) to decline
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

        // DC2: revert if ALL assignments are declined → back to Verified for LEO re-assignment
        var allDeclinedOrPending = reportAssignments
            .All(a => a.TeamId == request.TeamId
                ? true  // current one just declined
                : a.Status is AssignmentStatus.Assigned or AssignmentStatus.Declined);

        if (allDeclinedOrPending)
        {
            if (report.AssignedCompanyId is null)
            {
                // Community team flow — LEO re-assigns from Verified queue
                report.ForceStatus(ReportStatus.Verified);

                statusHistory.Add(ReportStatusHistory.Create(
                    report.Id,
                    ReportStatus.InProgress,
                    ReportStatus.Verified,
                    currentUser.UserId));
            }
            // Company-dispatched reports stay InProgress — CM re-assigns from company-queue
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        if (allDeclinedOrPending)
            logger.LogWarning("All teams declined report {ReportId} — reverted to Verified", report.Id);
        else
            logger.LogInformation("Team {TeamId} declined assignment for report {ReportId}",
                request.TeamId, report.Id);

        return Result.Success();
    }
}
