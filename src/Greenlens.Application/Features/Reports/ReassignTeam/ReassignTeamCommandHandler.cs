using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Options;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenlens.Application.Features.Reports.ReassignTeam;

/// <summary>Reassign report to different team (same type). BR-OFF-012, BR-ADM-010.</summary>
public sealed class ReassignTeamCommandHandler(
    IReportRepository reports,
    IEnvironmentalTeamRepository teams,
    IReportAssignmentRepository assignments,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ICleanupTaskAssignedNotifier taskNotifier,
    IOptions<WorkloadLimitsOptions> workloadOptions,
    IAuditLogger auditLogger,
    ILogger<ReassignTeamCommandHandler> logger) : IRequestHandler<ReassignTeamCommand, Result>
{
    public async Task<Result> Handle(ReassignTeamCommand request, CancellationToken ct)
    {
        logger.LogInformation("Reassigning team for report {ReportId}", request.ReportId);

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

        var oldTeam = await teams.GetByIdAsync(request.OldTeamId, ct).ConfigureAwait(false);
        var newTeam = await teams.GetByIdAsync(request.NewTeamId, ct).ConfigureAwait(false);

        if (oldTeam is null || newTeam is null)
        {
            logger.LogWarning("Team not found for ID {TeamId}", request.OldTeamId);
            return Errors.Organization.TeamNotFound;
        }

        // BR-OFF-012: same team type only
        if (oldTeam.TeamType != newTeam.TeamType)
        {
            logger.LogWarning("Old team type {OldTeamType} is not the same as new team type {NewTeamType}", oldTeam.TeamType, newTeam.TeamType);
            return Errors.Reports.ReassignSameTeamType;
        }

        // BR-OFF-013: configurable workload limit (default 6, warning at 5)
        var limits = workloadOptions.Value;
        var workload = await assignments.CountInProgressByTeamAsync(request.NewTeamId, ct).ConfigureAwait(false);
        if (workload >= limits.MaxTasksPerTeam)
        {
            logger.LogWarning("Team workload exceeded for team {TeamId}", request.NewTeamId);
            return Errors.Reports.TeamWorkloadExceeded;
        }

        // Find and update assignment
        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var oldAssignment = reportAssignments.FirstOrDefault(a => a.TeamId == request.OldTeamId);

        if (oldAssignment is null)
        {
            logger.LogWarning("Assignment not found for report {ReportId} and team {TeamId}", request.ReportId, request.OldTeamId);
            return Errors.Reports.AssignmentNotFound;
        }

        // Create new assignment, mark old as declined
        oldAssignment.Decline(request.Reason);

        var newAssignment = ReportAssignment.Create(
            request.ReportId,
            request.NewTeamId,
            currentUser.UserId,
            $"Reassigned from {request.OldTeamId}: {request.Reason}");

        assignments.Add(newAssignment);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "ReassignTeam",
            "Report",
            report.Id.ToString(),
            oldValues: JsonSerializer.Serialize(new { oldTeamId = request.OldTeamId }),
            newValues: JsonSerializer.Serialize(new
            {
                newTeamId = request.NewTeamId,
                reasonLength = request.Reason.Length
            }),
            ct).ConfigureAwait(false);

        await taskNotifier.NotifyTeamAsync(
            request.NewTeamId,
            report.Id,
            report.Code,
            ct).ConfigureAwait(false);

        logger.LogInformation("Report {ReportId} reassigned from team {OldTeamId} to {NewTeamId}",
            request.ReportId, request.OldTeamId, request.NewTeamId);

        return Result.Success();
    }
}
