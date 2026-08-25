using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.UpdateInspectionProgress;

/// <summary>
/// BR-INS-031: Update inspection progress while InProgress.
/// Must be updated ≥ 1 time/day. >24h → warning; >48h → escalate flag.
/// </summary>
public sealed class UpdateInspectionProgressCommandHandler(
    IInspectionReportRepository inspections,
    IReportRepository reports,
    ITeamMemberRepository teamMembers,
    IGeoDistanceService geoDistance,
    ISystemSettingsProvider systemSettings,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<UpdateInspectionProgressCommandHandler> logger)
    : IRequestHandler<UpdateInspectionProgressCommand, Result>
{
    public async Task<Result> Handle(UpdateInspectionProgressCommand request, CancellationToken ct)
    {
        logger.LogInformation("Getting update inspection progress");

        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
        {
            logger.LogWarning("Inspection not found for inspection {InspectionId}", request.InspectionId);
            return Errors.Inspections.InspectionNotFound;
        }

        // Must be team member
        var authError = await InspectionTeamAuthorization.ValidateTeamMemberAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
        {
            logger.LogWarning("Team member validation failed for inspection {InspectionId}", request.InspectionId);
            return authError;
        }

        var report = await reports.GetByIdAsync(inspection.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for inspection {InspectionId}", request.InspectionId);
            return Errors.Reports.ReportNotFound;
        }

        // BR-INS-031: progress updates must be submitted from near the site.
        var maxProgressDistanceMeters = ModuleSystemSettings.ProgressUpdateMaxDistanceMeters(systemSettings);
        var distance = await geoDistance.GetDistanceInMetersAsync(
            request.Latitude, request.Longitude,
            report.Latitude, report.Longitude, ct).ConfigureAwait(false);

        if (distance > maxProgressDistanceMeters)
        {
            logger.LogWarning("Distance {Distance} is greater than {MaxProgressDistanceMeters} for inspection {InspectionId}",
                distance, maxProgressDistanceMeters, request.InspectionId);
            return Errors.Progress.TooFarFromSite(distance);
        }

        var result = inspection.UpdateProgress(request.Percent, request.Note);
        if (result.IsFailure)
        {
            logger.LogWarning("Failed to update inspection progress for inspection {InspectionId}", request.InspectionId);
            return result;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Inspection {InspectionId} progress updated to {Percent}%",
            inspection.Id, request.Percent);

        return Result.Success();
    }
}
