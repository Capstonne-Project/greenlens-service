using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.AssignTeam;

/// <summary>
/// LEO assigns team(s) to a dispatched report. All teams are equal — no primary/secondary.
/// Validates team type against pollution category, checks workload limits.
/// Report transitions Dispatched → InProgress. Each assignment tracks independently.
/// Optionally tags waste types during assignment.
/// BR-OFF-011, BR-OFF-013, BR-ORG-013.
/// </summary>
public sealed class AssignTeamCommandHandler(
    IReportRepository reports,
    IEnvironmentalTeamRepository teams,
    IReportAssignmentRepository assignments,
    IReportStatusHistoryRepository statusHistory,
    IPollutionCategoryRepository categories,
    IWasteTagRepository wasteTags,
    IReportWasteTagRepository reportWasteTags,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<AssignTeamCommandHandler> logger) : IRequestHandler<AssignTeamCommand, Result>
{
    // Categories that route to Cleanup Team (BR-ORG-013)
    private static readonly HashSet<string> CleanupCategories = ["TRASH", "WASTEWATER", "CHEMICAL"];
    // Categories that route to Inspection Team (BR-ORG-013)
    private static readonly HashSet<string> InspectionCategories = ["NOISE", "AIR", "SMOKE"];

    public async Task<Result> Handle(AssignTeamCommand request, CancellationToken ct)
    {
        if (request.Teams.Count == 0)
            return Errors.Reports.AtLeastOneTeam;

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.Dispatched)
        {
            return report.Status == ReportStatus.InProgress
                ? Errors.Reports.ReportAlreadyAssigned
                : Errors.Reports.InvalidStatusTransition;
        }

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

            // BR-OFF-013: team can only handle 1 task at a time
            var workload = await assignments.CountInProgressByTeamAsync(item.TeamId, ct).ConfigureAwait(false);
            if (workload >= 1)
                return Errors.Reports.TeamWorkloadExceeded;
        }

        // Validate and persist waste tags if provided
        if (request.WasteTagIds is { Count: > 0 })
        {
            var tags = await wasteTags.GetByIdsAsync(request.WasteTagIds, ct).ConfigureAwait(false);
            if (tags.Count != request.WasteTagIds.Count)
                return Errors.Reports.WasteTagNotFound;

            var inactiveTags = tags.Where(t => !t.IsActive).ToList();
            if (inactiveTags.Count > 0)
                return Errors.Reports.WasteTagInactive;

            // Remove existing tags, then add new ones
            var existing = await reportWasteTags.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
            if (existing.Count > 0)
                reportWasteTags.RemoveRange(existing);

            var newTags = request.WasteTagIds
                .Select(tagId => ReportWasteTag.Create(request.ReportId, tagId, currentUser.UserId))
                .ToList();

            reportWasteTags.AddRange(newTags);
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

        // Transition report: Dispatched → InProgress
        report.Assign(currentUser.UserId);

        var history = ReportStatusHistory.Create(
            report.Id,
            ReportStatus.Dispatched,
            ReportStatus.InProgress,
            currentUser.UserId);

        statusHistory.Add(history);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Report {ReportId} assigned to {TeamCount} team(s) by LEO {UserId}",
            report.Id, request.Teams.Count, currentUser.UserId);

        return Result.Success();
    }
}
