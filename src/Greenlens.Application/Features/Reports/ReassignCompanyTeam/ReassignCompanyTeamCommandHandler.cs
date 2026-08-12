using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Options;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Notifications;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenlens.Application.Features.Reports.ReassignCompanyTeam;

/// <summary>
/// CompanyManager reassigns report to a different team within their company.
/// Supports replacing a team that declined (assignment already Declined) or proactively swapping Assigned teams.
/// Report remains InProgress throughout.
/// </summary>
/// <remarks>Implements: BR-CMP-005, BR-CMP-021, BR-CLN-007, BR-OFF-012, BR-ADM-010.</remarks>
public sealed class ReassignCompanyTeamCommandHandler(
    IReportRepository reports,
    IEnvironmentalTeamRepository teams,
    ITeamMemberRepository teamMembers,
    IReportAssignmentRepository assignments,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ICleanupTaskAssignedNotifier taskNotifier,
    IOptions<WorkloadLimitsOptions> workloadOptions,
    IAuditLogger auditLogger,
    ILogger<ReassignCompanyTeamCommandHandler> logger) : IRequestHandler<ReassignCompanyTeamCommand, Result>
{
    public async Task<Result> Handle(ReassignCompanyTeamCommand request, CancellationToken ct)
    {
        logger.LogInformation("CM reassigning team for report {ReportId}", request.ReportId);

        if (request.Reason.Length < 20)
        {
            logger.LogWarning("Reason is too short for report {ReportId}", request.ReportId);
            return Errors.Reports.ReasonTooShort;
        }

        var companyIdResult = await CompanyContextResolver
            .ResolveCompanyIdAsync(companyStaff, currentUser.UserId, ct)
            .ConfigureAwait(false);
        if (companyIdResult.IsFailure)
            return companyIdResult.Error!;

        var companyId = companyIdResult.Value;

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

        if (report.AssignedCompanyId != companyId)
            return Errors.Reports.ReportNotDispatchedToYourCompany;

        var oldTeam = await teams.GetByIdAsync(request.OldTeamId, ct).ConfigureAwait(false);
        var newTeam = await teams.GetByIdAsync(request.NewTeamId, ct).ConfigureAwait(false);

        if (oldTeam is null || newTeam is null)
        {
            logger.LogWarning("Team not found for reassignment on report {ReportId}", request.ReportId);
            return Errors.Organization.TeamNotFound;
        }

        if (oldTeam.CompanyId != companyId || newTeam.CompanyId != companyId)
        {
            logger.LogWarning("Teams must belong to caller company {CompanyId}", companyId);
            return Errors.Reports.ReportNotDispatchedToYourCompany;
        }

        if (!await teamMembers.HasMembersAsync(request.NewTeamId, ct).ConfigureAwait(false))
        {
            logger.LogWarning("Team {TeamId} has no members", request.NewTeamId);
            return Errors.Organization.TeamHasNoMembers;
        }

        var limits = workloadOptions.Value;
        var workload = await assignments.CountInProgressByTeamAsync(request.NewTeamId, ct).ConfigureAwait(false);
        if (workload >= limits.MaxTasksPerTeam)
        {
            logger.LogWarning("Team workload exceeded for team {TeamId}", request.NewTeamId);
            return Errors.Reports.TeamWorkloadExceeded;
        }

        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var oldAssignment = ReportAssignmentSelection.SelectLatestForTeam(reportAssignments, request.OldTeamId);

        if (oldAssignment is null)
        {
            logger.LogWarning(
                "Assignment not found for report {ReportId} and team {TeamId}",
                request.ReportId, request.OldTeamId);
            return Errors.Reports.AssignmentNotFound;
        }

        if (request.OldTeamId == request.NewTeamId)
        {
            logger.LogWarning("Reassign skipped: old and new team are the same ({TeamId})", request.NewTeamId);
            return Errors.Reports.InvalidStatusTransition;
        }

        if (reportAssignments.Any(a =>
                a.TeamId != request.OldTeamId
                && a.Status == AssignmentStatus.InProgress))
        {
            logger.LogWarning(
                "Cannot reassign report {ReportId}: another team assignment is InProgress",
                request.ReportId);
            return Errors.Reports.InvalidStatusTransition;
        }

        if (oldAssignment.Status == AssignmentStatus.Assigned)
            oldAssignment.Decline(request.Reason);
        else if (oldAssignment.Status != AssignmentStatus.Declined)
        {
            logger.LogWarning(
                "Cannot reassign from assignment {AssignmentId} in status {Status}",
                oldAssignment.Id, oldAssignment.Status);
            return Errors.Reports.InvalidStatusTransition;
        }

        if (ReportAssignmentSelection.HasOpenAssignmentForTeam(reportAssignments, request.NewTeamId))
        {
            logger.LogWarning(
                "Team {TeamId} already has an open assignment on report {ReportId}",
                request.NewTeamId, request.ReportId);
            return Errors.Reports.TeamAlreadyAssignedOnReport;
        }

        var newAssignment = ReportAssignment.Create(
            request.ReportId,
            request.NewTeamId,
            currentUser.UserId,
            $"Reassigned from {request.OldTeamId}: {request.Reason}");

        assignments.Add(newAssignment);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "ReassignCompanyTeam",
            "Report",
            report.Id.ToString(),
            oldValues: JsonSerializer.Serialize(new { oldTeamId = request.OldTeamId }),
            newValues: JsonSerializer.Serialize(new
            {
                newTeamId = request.NewTeamId,
                companyId,
                reasonLength = request.Reason.Length
            }),
            ct).ConfigureAwait(false);

        await taskNotifier.NotifyTeamAsync(
            request.NewTeamId,
            report.Id,
            report.Code,
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Report {ReportId} reassigned by CM {UserId} from team {OldTeamId} to {NewTeamId}",
            request.ReportId, currentUser.UserId, request.OldTeamId, request.NewTeamId);

        return Result.Success();
    }
}
