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
    ILogger<AssignTeamCommandHandler> logger) : IRequestHandler<AssignTeamCommand, Result>
{
    public async Task<Result> Handle(AssignTeamCommand request, CancellationToken ct)
    {
        if (request.Teams.Count == 0)
            return Errors.Reports.AtLeastOneTeam;

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        // v3.0: Verified → InProgress (no more Dispatched step)
        if (report.Status != ReportStatus.Verified)
        {
            return report.Status == ReportStatus.InProgress
                ? Errors.Reports.ReportAlreadyAssigned
                : Errors.Reports.InvalidStatusTransition;
        }

        // Validate each team
        foreach (var item in request.Teams)
        {
            var team = await teams.GetByIdAsync(item.TeamId, ct).ConfigureAwait(false);
            if (team is null)
                return Errors.Organization.TeamNotFound;

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
