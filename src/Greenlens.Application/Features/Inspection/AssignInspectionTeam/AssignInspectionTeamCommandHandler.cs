using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.AssignInspectionTeam;

/// <summary>
/// LEO assigns an Inspector Team to an existing InspectionReport.
/// If the parent Report is still Verified, transitions it to InProgress.
/// </summary>
/// <remarks>Implements: BR-INS-001, BR-OFF-005.</remarks>
public sealed class AssignInspectionTeamCommandHandler(
    IInspectionReportRepository inspections,
    IReportRepository reports,
    IEnvironmentalTeamRepository teams,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<AssignInspectionTeamCommandHandler> logger)
    : IRequestHandler<AssignInspectionTeamCommand, Result>
{
    public async Task<Result> Handle(AssignInspectionTeamCommand request, CancellationToken ct)
    {
        logger.LogInformation("Getting assign inspection team");

        // 1. Validate inspection exists
        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
        {
            logger.LogWarning("Inspection not found for inspection {InspectionId}", request.InspectionId);
            return Errors.Inspections.InspectionNotFound;
        }

        // 2. Validate team exists and is of type Inspection
        var team = await teams.GetByIdAsync(request.TeamId, ct).ConfigureAwait(false);
        if (team is null)
        {
            logger.LogWarning("Team not found for team {TeamId}", request.TeamId);
            return Errors.Inspections.TeamNotFound;
        }

        if (team.TeamType != TeamType.Inspection)
        {
            logger.LogWarning("Team {TeamId} is not of type Inspection", request.TeamId);
            return Errors.Inspections.TeamNotInspectionType;
        }

        // 3. Assign team via domain method (validates status = Draft or InProgress)
        var assignResult = inspection.AssignTeam(request.TeamId);
        if (!assignResult.IsSuccess)
        {
            logger.LogWarning("Failed to assign team {TeamId} to inspection {InspectionId}", request.TeamId, request.InspectionId);
            return assignResult;
        }

        // 4. Transition parent Report: Verified → InProgress (if still Verified)
        var report = await reports.GetByIdAsync(inspection.ReportId, ct).ConfigureAwait(false);
        if (report is not null && report.Status == ReportStatus.Verified)
        {
            report.Assign(currentUser.UserId);

            var history = ReportStatusHistory.Create(
                report.Id,
                ReportStatus.Verified,
                ReportStatus.InProgress,
                currentUser.UserId);

            statusHistory.Add(history);
        }

        // 5. Persist
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "InspectionReport {InspectionId} assigned to Inspector Team {TeamId} by LEO {UserId}",
            request.InspectionId, request.TeamId, currentUser.UserId);

        return Result.Success();
    }
}
