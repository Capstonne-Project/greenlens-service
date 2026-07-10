using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Options;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenlens.Application.Features.Reports.ReassignTeam;

/// <summary>Reassign report to different team (same type). BR-OFF-012.</summary>
public sealed class ReassignTeamCommandHandler(
    IReportRepository reports,
    IEnvironmentalTeamRepository teams,
    IReportAssignmentRepository assignments,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    IOptions<WorkloadLimitsOptions> workloadOptions,
    ILogger<ReassignTeamCommandHandler> logger) : IRequestHandler<ReassignTeamCommand, Result>
{
    public async Task<Result> Handle(ReassignTeamCommand request, CancellationToken ct)
    {
        if (request.Reason.Length < 20)
            return Errors.Reports.ReasonTooShort;

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        var oldTeam = await teams.GetByIdAsync(request.OldTeamId, ct).ConfigureAwait(false);
        var newTeam = await teams.GetByIdAsync(request.NewTeamId, ct).ConfigureAwait(false);

        if (oldTeam is null || newTeam is null)
            return Errors.Organization.TeamNotFound;

        // BR-OFF-012: same team type only
        if (oldTeam.TeamType != newTeam.TeamType)
            return Errors.Reports.ReassignSameTeamType;

        // BR-OFF-013: configurable workload limit (default 6, warning at 5)
        var limits = workloadOptions.Value;
        var workload = await assignments.CountInProgressByTeamAsync(request.NewTeamId, ct).ConfigureAwait(false);
        if (workload >= limits.MaxTasksPerTeam)
            return Errors.Reports.TeamWorkloadExceeded;

        // Find and update assignment
        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var oldAssignment = reportAssignments.FirstOrDefault(a => a.TeamId == request.OldTeamId);

        if (oldAssignment is null)
            return Errors.Reports.AssignmentNotFound;

        // Create new assignment, mark old as declined
        oldAssignment.Decline(request.Reason);

        var newAssignment = ReportAssignment.Create(
            request.ReportId,
            request.NewTeamId,
            currentUser.UserId,
            $"Reassigned from {request.OldTeamId}: {request.Reason}");

        assignments.Add(newAssignment);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Report {ReportId} reassigned from team {OldTeamId} to {NewTeamId}",
            request.ReportId, request.OldTeamId, request.NewTeamId);

        return Result.Success();
    }
}
