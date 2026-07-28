using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CommunityCleanup.CheckInCommunityCleanup;

/// <remarks>Draft rule BR-CMU-007: check-in ≤ 200m from meeting point (or report location).</remarks>
public sealed class CheckInCommunityCleanupCommandHandler(
    ICommunityCleanupEventRepository events,
    ICommunityCleanupParticipantRepository participants,
    IReportRepository reports,
    IGeoDistanceService geoDistance,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<CheckInCommunityCleanupCommandHandler> logger) : IRequestHandler<CheckInCommunityCleanupCommand, Result>
{
    private const double MaxCheckInDistanceMeters = 200;

    public async Task<Result> Handle(CheckInCommunityCleanupCommand request, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(request.EventId, ct).ConfigureAwait(false);
        if (ev is null)
            return Errors.CommunityCleanup.EventNotFound;

        if (ev.Status is not (CommunityCleanupStatus.OpenForJoin or CommunityCleanupStatus.JoinClosed or CommunityCleanupStatus.InProgress))
            return Errors.CommunityCleanup.InvalidStatusTransition;

        var participant = await participants.GetByEventAndUserAsync(request.EventId, currentUser.UserId, ct).ConfigureAwait(false);
        if (participant is null)
            return Errors.CommunityCleanup.ParticipantNotFound;

        var report = await reports.GetByIdAsync(ev.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        var targetLat = ev.MeetingLatitude ?? report.Latitude;
        var targetLng = ev.MeetingLongitude ?? report.Longitude;

        var distance = await geoDistance.GetDistanceInMetersAsync(
            request.Latitude, request.Longitude, targetLat, targetLng, ct).ConfigureAwait(false);

        if (distance > MaxCheckInDistanceMeters)
        {
            logger.LogWarning("Check-in distance {Distance}m exceeds {Max}m for event {EventId}", distance, MaxCheckInDistanceMeters, request.EventId);
            return Errors.CommunityCleanup.CheckInTooFar;
        }

        try
        {
            participant.CheckIn(request.Latitude, request.Longitude);
        }
        catch (InvalidOperationException)
        {
            return Errors.CommunityCleanup.InvalidStatusTransition;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("User {UserId} checked in for community cleanup {EventId} at {Distance:F1}m", currentUser.UserId, request.EventId, distance);
        return Result.Success();
    }
}
