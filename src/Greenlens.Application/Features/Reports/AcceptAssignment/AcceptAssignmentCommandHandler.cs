using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.AcceptAssignment;

/// <summary>
/// Team leader accepts the assignment. Assigned → InProgress. StartedAt set here.
/// Implements: BR-CLN-001, BR-INS-001.
/// </summary>
public sealed class AcceptAssignmentCommandHandler(
    IReportRepository reports,
    IReportAssignmentRepository assignments,
    ITeamMemberRepository teamMembers,
    ICleanupAssignmentActivityNotifier activityNotifier,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<AcceptAssignmentCommandHandler> logger) : IRequestHandler<AcceptAssignmentCommand, Result>
{
    public async Task<Result> Handle(AcceptAssignmentCommand request, CancellationToken ct)
    {
        logger.LogInformation("Accepting assignment for report {ReportId}", request.ReportId);

        var leader = await teamMembers.GetLeaderByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (leader is null)
        {
            logger.LogWarning("Team leader not found for user ID {UserId}", currentUser.UserId);
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
            logger.LogWarning("Report {ReportId} is not in progress", request.ReportId);
            return Errors.Reports.InvalidStatusTransition;
        }

        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var assignment = ReportAssignmentSelection.SelectLatestForTeam(reportAssignments, leader.TeamId);

        if (assignment is null)
        {
            logger.LogWarning("Assignment not found for report ID {ReportId} and team ID {TeamId}", request.ReportId, leader.TeamId);
            return Errors.Reports.AssignmentNotFound;
        }

        if (assignment.Status != AssignmentStatus.Assigned)
        {
            logger.LogWarning("Assignment {AssignmentId} is not assigned", assignment.Id);
            return Errors.Reports.InvalidStatusTransition;
        }

        assignment.Accept();

        // Mark report StartedAt on first accept
        report.MarkStarted();

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await activityNotifier.NotifyAcceptedAsync(
            assignment.AssignedById,
            leader.TeamId,
            report.Id,
            report.Code,
            ct).ConfigureAwait(false);

        logger.LogInformation("Assignment accepted: team {TeamId} for report {ReportId}",
            leader.TeamId, request.ReportId);

        return Result.Success();
    }
}
