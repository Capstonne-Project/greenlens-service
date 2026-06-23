using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.AssignCompanyTeam;

/// <summary>
/// CompanyManager assigns their company's team(s) to a report dispatched to their company.
/// Validates: report dispatched to caller's company, teams belong to that company, workload ok.
/// Transitions report: Verified → InProgress.
/// </summary>
/// <remarks>Implements: BR-CMP-005, BR-OFF-011.</remarks>
public sealed class AssignCompanyTeamCommandHandler(
    IReportRepository reports,
    IEnvironmentalTeamRepository teams,
    IReportAssignmentRepository assignments,
    IReportStatusHistoryRepository statusHistory,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<AssignCompanyTeamCommandHandler> logger) : IRequestHandler<AssignCompanyTeamCommand, Result>
{
    public async Task<Result> Handle(AssignCompanyTeamCommand request, CancellationToken ct)
    {
        if (request.Teams.Count == 0)
            return Errors.Reports.AtLeastOneTeam;

        // Resolve caller's company
        var staff = await companyStaff.GetByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (staff is null || !staff.IsActive)
            return Errors.Reports.ReportNotDispatchedToYourCompany;

        var callerCompanyId = staff.CompanyId;

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        // Must be Verified and dispatched to caller's company
        if (report.Status != ReportStatus.Verified)
            return Errors.Reports.InvalidStatusTransition;

        if (report.AssignedCompanyId != callerCompanyId)
            return Errors.Reports.ReportNotDispatchedToYourCompany;

        // Validate each team
        foreach (var item in request.Teams)
        {
            var team = await teams.GetByIdAsync(item.TeamId, ct).ConfigureAwait(false);
            if (team is null)
                return Errors.Organization.TeamNotFound;

            // Team must belong to caller's company
            if (team.CompanyId != callerCompanyId)
                return Errors.Reports.ReportNotDispatchedToYourCompany;

            // BR-OFF-013: team can only handle 1 task at a time
            var workload = await assignments.CountInProgressByTeamAsync(item.TeamId, ct).ConfigureAwait(false);
            if (workload >= 1)
                return Errors.Reports.TeamWorkloadExceeded;
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

        // Transition: Verified → InProgress (via CM)
        report.AssignByCompanyManager(currentUser.UserId);

        var history = ReportStatusHistory.Create(
            report.Id,
            ReportStatus.Verified,
            ReportStatus.InProgress,
            currentUser.UserId);

        statusHistory.Add(history);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Report {ReportId} assigned to {TeamCount} company team(s) by CompanyManager {UserId}",
            report.Id, request.Teams.Count, currentUser.UserId);

        return Result.Success();
    }
}
