using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.CheckInInspection;

/// <summary>
/// BR-INS-004: Check-in ≤ 200m (PostGIS).
/// Draft → InProgress. Records GPS location.
/// </summary>
public sealed class CheckInInspectionCommandHandler(
    IInspectionReportRepository inspections,
    IReportRepository reports,
    ITeamMemberRepository teamMembers,
    IGeoDistanceService geoDistance,
    ISystemSettingsProvider systemSettings,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<CheckInInspectionCommandHandler> logger)
    : IRequestHandler<CheckInInspectionCommand, Result>
{
    public async Task<Result> Handle(CheckInInspectionCommand request, CancellationToken ct)
    {
        var maxCheckInDistanceMeters = ModuleSystemSettings.CheckInMaxDistanceMeters(systemSettings);

        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
        {
            logger.LogWarning("Inspection not found for inspection {InspectionId}", request.InspectionId);
            return Errors.Inspections.InspectionNotFound;
        }

        // Verify current user belongs to assigned team
        var authError = await InspectionTeamAuthorization.ValidateTeamMemberAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
        {
            logger.LogWarning("Team member validation failed for inspection {InspectionId}", request.InspectionId);
            return authError;
        }

        // Get report location for distance check
        var report = await reports.GetByIdAsync(inspection.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for inspection {InspectionId}", request.InspectionId);
            return Errors.Reports.ReportNotFound;
        }

        // BR-INS-004: PostGIS distance check ≤ 200m
        var distance = await geoDistance.GetDistanceInMetersAsync(
            request.Latitude, request.Longitude,
            report.Latitude, report.Longitude, ct).ConfigureAwait(false);

        if (distance > maxCheckInDistanceMeters)
        {
            logger.LogWarning("Distance check failed for inspection {InspectionId}. Distance: {Distance}m", request.InspectionId, distance);
            return Errors.Inspections.TooFarFromSite(distance);
        }

        // Draft → InProgress
        var result = inspection.CheckIn(request.Latitude, request.Longitude, request.Note);
        if (result.IsFailure)
        {
            logger.LogWarning("Check in failed for inspection {InspectionId}. Result: {Result}", request.InspectionId, result.Error);
            return result;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Inspection {InspectionId} checked in at {Distance:F1}m from report {ReportId}",
            inspection.Id, distance, report.Id);

        return Result.Success();
    }
}
