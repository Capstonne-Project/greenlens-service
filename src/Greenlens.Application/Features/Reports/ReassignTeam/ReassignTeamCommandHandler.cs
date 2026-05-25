using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.ReassignTeam;

/// <summary>Reassign report to different team (same type). BR-OFF-012.</summary>
public sealed class ReassignTeamCommandHandler(
    IReportRepository reports,
    IEnvironmentalTeamRepository teams,
    IReportAssignmentRepository assignments,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow) : IRequestHandler<ReassignTeamCommand, Result>
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

        // Check workload
        var workload = await assignments.CountInProgressByTeamAsync(request.NewTeamId, ct).ConfigureAwait(false);
        if (workload >= 10)
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

        return Result.Success();
    }
}
