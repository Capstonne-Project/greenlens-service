using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.CheckInCleanup;

/// <summary>
/// BR-CLN-002: Check-in ≤ 200m (PostGIS ST_Distance).
/// BR-CLN-003: Check-in to start task.
/// Records GPS and transitions assignment Assigned → InProgress.
/// </summary>
public sealed class CheckInCleanupCommandHandler(
    IReportRepository reports,
    IReportAssignmentRepository assignments,
    IGeoDistanceService geoDistance,
    IUnitOfWork uow,
    ILogger<CheckInCleanupCommandHandler> logger)
    : IRequestHandler<CheckInCleanupCommand, Result>
{
    private const double MaxCheckInDistanceMeters = 200;

    public async Task<Result> Handle(CheckInCleanupCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.InProgress)
            return Errors.Reports.InvalidStatusTransition;

        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var assignment = reportAssignments.FirstOrDefault(a => a.TeamId == request.TeamId);

        if (assignment is null)
            return Errors.Reports.AssignmentNotFound;

        if (assignment.Status != AssignmentStatus.Assigned)
            return Errors.Reports.InvalidStatusTransition;

        // BR-CLN-002: PostGIS distance check ≤ 200m
        var distance = await geoDistance.GetDistanceInMetersAsync(
            request.Latitude, request.Longitude,
            report.Latitude, report.Longitude, ct).ConfigureAwait(false);

        if (distance > MaxCheckInDistanceMeters)
            return Errors.Cleanup.TooFarFromSite;

        // BR-CLN-003: Check-in transitions to InProgress
        assignment.CheckIn(request.Latitude, request.Longitude, request.Note);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Team {TeamId} checked in for report {ReportId} at {Distance:F1}m",
            request.TeamId, report.Id, distance);

        return Result.Success();
    }
}
