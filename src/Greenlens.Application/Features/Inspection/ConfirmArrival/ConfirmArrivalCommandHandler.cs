using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.ConfirmArrival;

/// <summary>
/// BR-INS-033: Optional soft GPS arrival. ≤200m OK; &gt;200m requires explanatory note.
/// </summary>
/// <remarks>Implements: BR-INS-033.</remarks>
public sealed class ConfirmArrivalCommandHandler(
    IInspectionReportRepository inspections,
    IReportRepository reports,
    ITeamMemberRepository teamMembers,
    IGeoDistanceService geoDistance,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<ConfirmArrivalCommandHandler> logger)
    : IRequestHandler<ConfirmArrivalCommand, Result>
{
    private const double SoftGpsThresholdMeters = 200;

    public async Task<Result> Handle(ConfirmArrivalCommand request, CancellationToken ct)
    {
        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
            return Errors.Inspections.InspectionNotFound;

        var authError = await InspectionTeamAuthorization.ValidateTeamMemberAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
            return authError;

        var report = await reports.GetByIdAsync(inspection.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        var distance = await geoDistance.GetDistanceInMetersAsync(
            request.Latitude, request.Longitude,
            report.Latitude, report.Longitude, ct).ConfigureAwait(false);

        if (distance > SoftGpsThresholdMeters && string.IsNullOrWhiteSpace(request.Note))
        {
            logger.LogWarning(
                "Arrival note required for inspection {InspectionId} — distance {Distance}m",
                request.InspectionId, distance);
            return Errors.Inspections.ArrivalNoteRequiredWhenFar;
        }

        var result = inspection.ConfirmArrival(request.Latitude, request.Longitude, request.Note);
        if (result.IsFailure)
            return result;

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Inspection {InspectionId} arrival confirmed at {Distance:F1}m from report",
            inspection.Id, distance);

        return Result.Success();
    }
}
