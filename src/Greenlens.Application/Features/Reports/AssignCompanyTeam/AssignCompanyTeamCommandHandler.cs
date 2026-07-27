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

namespace Greenlens.Application.Features.Reports.AssignCompanyTeam;

/// <summary>
/// CompanyManager assigns their company's team(s) to a report dispatched to their company.
/// Validates: report InProgress + dispatched to caller's company, teams belong to that company, workload ok.
/// Report status remains InProgress (set at LEO dispatch).
/// </summary>
/// <remarks>Implements: BR-CMP-005, BR-OFF-011.</remarks>
public sealed class AssignCompanyTeamCommandHandler(
    IReportRepository reports,
    IEnvironmentalTeamRepository teams,
    IReportAssignmentRepository assignments,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ICleanupTaskAssignedNotifier taskNotifier,
    IOptions<WorkloadLimitsOptions> workloadOptions,
    ILogger<AssignCompanyTeamCommandHandler> logger) : IRequestHandler<AssignCompanyTeamCommand, Result>
{
    public async Task<Result> Handle(AssignCompanyTeamCommand request, CancellationToken ct)
    {
        
        if (request.Teams.Count == 0)
        {
            logger.LogWarning("No teams provided for report {ReportId}", request.ReportId);
            return Errors.Reports.AtLeastOneTeam;
        }

        // Resolve caller's company
        var staff = await companyStaff.GetByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (staff is null || !staff.IsActive)
        {
            logger.LogWarning("Company staff not found for user ID {UserId}", currentUser.UserId);
            return Errors.Reports.ReportNotDispatchedToYourCompany;
        }

        var callerCompanyId = staff.CompanyId;

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        // Must be InProgress and dispatched to caller's company
        if (report.Status != ReportStatus.InProgress)
        {
            logger.LogWarning("Report {ReportId} is not in progress", request.ReportId);
            return Errors.Reports.InvalidStatusTransition;
        }
        if (report.AssignedCompanyId != callerCompanyId)
        {
            logger.LogWarning("Report {ReportId} is not dispatched to caller's company", request.ReportId);
            return Errors.Reports.ReportNotDispatchedToYourCompany;
        }
        // Validate each team
        foreach (var item in request.Teams)
        {
            var team = await teams.GetByIdAsync(item.TeamId, ct).ConfigureAwait(false);
            if (team is null)
            {
                logger.LogWarning("Team not found for ID {TeamId}", item.TeamId);
                return Errors.Organization.TeamNotFound;
            }

            // Team must belong to caller's company
            if (team.CompanyId != callerCompanyId)
            {
                logger.LogWarning("Team {TeamId} is not in caller's company", item.TeamId);
                return Errors.Reports.ReportNotDispatchedToYourCompany;
            }

            // BR-OFF-013: configurable workload limit (default 6, warning at 5)
            var limits = workloadOptions.Value;
            var workload = await assignments.CountInProgressByTeamAsync(item.TeamId, ct).ConfigureAwait(false);
            if (workload >= limits.MaxTasksPerTeam)
            {
                logger.LogWarning("Team {TeamId} workload exceeded: {Current}/{Max}", item.TeamId, workload, limits.MaxTasksPerTeam);
                return Errors.Reports.TeamWorkloadExceeded;
            }
            if (workload >= limits.WarningThreshold)
                logger.LogWarning("Company team {TeamId} approaching workload limit: {Current}/{Max}",
                    item.TeamId, workload, limits.MaxTasksPerTeam);
        }

        // Create assignments
        foreach (var item in request.Teams)
        {
            var assignment = ReportAssignment.Create(
                report.Id,
                item.TeamId,
                currentUser.UserId,
                item.Note);

            assignments.Add(assignment);
        }

        // Record CM as assigner; status already InProgress from LEO dispatch
        report.AssignByCompanyManager(currentUser.UserId);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        foreach (var item in request.Teams)
        {
            await taskNotifier.NotifyTeamAsync(
                item.TeamId,
                report.Id,
                report.Code,
                ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Report {ReportId} assigned to {TeamCount} company team(s) by CompanyManager {UserId}",
            report.Id, request.Teams.Count, currentUser.UserId);

        return Result.Success();
    }
}
