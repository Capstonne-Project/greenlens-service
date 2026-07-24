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

namespace Greenlens.Application.Features.Reports.AssignTeam;

/// <summary>
/// LEO assigns team(s) to a verified report. All teams are equal — no primary/secondary.
/// Dispatch is by NEED (v1.3) — LEO chooses team type freely, not constrained by category.
/// Report transitions Verified → InProgress. Each assignment tracks independently.
/// Optionally tags waste types during assignment.
/// BR-OFF-011, BR-OFF-013.
/// </summary>
public sealed class AssignTeamCommandHandler(
    IReportRepository reports,
    IEnvironmentalTeamRepository teams,
    IReportAssignmentRepository assignments,
    IReportStatusHistoryRepository statusHistory,
    IWasteTagRepository wasteTags,
    IReportWasteTagRepository reportWasteTags,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    IOptions<WorkloadLimitsOptions> workloadOptions,
    ILogger<AssignTeamCommandHandler> logger) : IRequestHandler<AssignTeamCommand, Result>
{
    public async Task<Result> Handle(AssignTeamCommand request, CancellationToken ct)
    {
        if (request.Teams.Count == 0)
        {
            logger.LogWarning("No teams provided for report {ReportId}", request.ReportId);
            return Errors.Reports.AtLeastOneTeam;
        }

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        // v3.0: Verified → InProgress (no more Dispatched step)
        if (report.Status != ReportStatus.Verified)
        {
            logger.LogWarning("Report {ReportId} is not verified", request.ReportId);
            return report.Status == ReportStatus.InProgress
                ? Errors.Reports.ReportAlreadyAssigned
                : Errors.Reports.InvalidStatusTransition;
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

            // Guard: LEO cannot assign company teams directly — must use dispatch-to-company flow
            if (team.IsCompanyTeam)
            {
                logger.LogWarning("Team {TeamId} is a company team", item.TeamId);
                return Errors.Reports.CannotAssignCompanyTeamDirectly;
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
                logger.LogWarning("Team {TeamId} approaching workload limit: {Current}/{Max}",
                    item.TeamId, workload, limits.MaxTasksPerTeam);
        }

        // Validate and persist waste tags if provided
        if (request.WasteTagIds is { Count: > 0 })
        {
            var tags = await wasteTags.GetByIdsAsync(request.WasteTagIds, ct).ConfigureAwait(false);
            if (tags.Count != request.WasteTagIds.Count)
            {
                logger.LogWarning("Waste tags not found for IDs {WasteTagIds}", request.WasteTagIds);
                return Errors.Reports.WasteTagNotFound;
            }

            var inactiveTags = tags.Where(t => !t.IsActive).ToList();
            if (inactiveTags.Count > 0)
            {
                logger.LogWarning("Waste tags inactive for IDs {WasteTagIds}", inactiveTags.Select(t => t.Id));
                return Errors.Reports.WasteTagInactive;
            }

            // Remove existing tags, then add new ones
            var existing = await reportWasteTags.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
            if (existing.Count > 0)
            {
                logger.LogWarning("Existing waste tags found for report {ReportId}", request.ReportId);
                reportWasteTags.RemoveRange(existing);
            }

            var newTags = request.WasteTagIds
                .Select(tagId => ReportWasteTag.Create(request.ReportId, tagId, currentUser.UserId))
                .ToList();

            if (newTags.Count > 0)
            {
                logger.LogWarning("New waste tags added for report {ReportId}", request.ReportId);
                reportWasteTags.AddRange(newTags);
            }
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

        logger.LogInformation("Report {ReportId} assigned to {TeamCount} team(s) by LEO {UserId}",
            report.Id, request.Teams.Count, currentUser.UserId);

        return Result.Success();
    }
}
