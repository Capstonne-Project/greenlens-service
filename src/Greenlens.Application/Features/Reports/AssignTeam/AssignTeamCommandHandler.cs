using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.AssignTeam;

/// <summary>
/// LEO assigns team(s) to a verified report. All teams are equal — no primary/secondary.
/// Validates team type against pollution category, checks workload limits.
/// Report transitions Verified → InProgress. Each assignment tracks independently.
/// BR-OFF-011, BR-OFF-013, BR-ORG-013.
/// </summary>
public sealed class AssignTeamCommandHandler(
    IReportRepository reports,
    IEnvironmentalTeamRepository teams,
    IReportAssignmentRepository assignments,
    IReportStatusHistoryRepository statusHistory,
    IPollutionCategoryRepository categories,
    ICurrentUser currentUser,
    IUnitOfWork uow) : IRequestHandler<AssignTeamCommand, Result>
{
    // Categories that route to Cleanup Team (BR-ORG-013)
    private static readonly HashSet<string> CleanupCategories = ["TRASH", "WASTEWATER", "CHEMICAL"];
    // Categories that route to Inspection Team (BR-ORG-013)
    private static readonly HashSet<string> InspectionCategories = ["NOISE", "AIR"];

    public async Task<Result> Handle(AssignTeamCommand request, CancellationToken ct)
    {
        if (request.Teams.Count == 0)
            return Errors.Reports.AtLeastOneTeam;

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.Verified)
            return Errors.Reports.InvalidStatusTransition;

        // Load pollution category to determine expected team type
        var category = await categories.GetByIdAsync(report.CategoryId, ct).ConfigureAwait(false);
        if (category is null)
            return Errors.Reports.CategoryNotFound;

        var expectedTeamType = CleanupCategories.Contains(category.Code.ToUpperInvariant())
            ? TeamType.Cleanup
            : InspectionCategories.Contains(category.Code.ToUpperInvariant())
                ? TeamType.Inspection
                : (TeamType?)null;

        // Validate each team
        foreach (var item in request.Teams)
        {
            var team = await teams.GetByIdAsync(item.TeamId, ct).ConfigureAwait(false);
            if (team is null)
                return Errors.Organization.TeamNotFound;

            // BR-ORG-013: team type must match pollution category
            if (expectedTeamType.HasValue && team.TeamType != expectedTeamType.Value)
                return Errors.Reports.TeamTypeMismatch;

            // BR-OFF-013: workload limit (10 in-progress per team)
            var workload = await assignments.CountInProgressByTeamAsync(item.TeamId, ct).ConfigureAwait(false);
            if (workload >= 10)
                return Errors.Reports.TeamWorkloadExceeded;
        }

        // Create assignments — all teams equal
        foreach (var item in request.Teams)
        {
            var assignment = ReportAssignment.Create(
                report.Id,
                item.TeamId,
                currentUser.UserId,
                item.Note);

            assignments.Add(assignment);
        }

        // Transition report: Verified → InProgress
        report.Assign(currentUser.UserId);

        var history = ReportStatusHistory.Create(
            report.Id,
            ReportStatus.Verified,
            ReportStatus.InProgress,
            currentUser.UserId);

        statusHistory.Add(history);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}
