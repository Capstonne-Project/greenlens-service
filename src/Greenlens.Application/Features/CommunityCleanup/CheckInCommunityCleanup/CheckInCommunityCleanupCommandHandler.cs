using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CommunityCleanup.CheckInCommunityCleanup;

/// <remarks>
/// Draft rule BR-CMU-007: check-in ≤ 200m from meeting point (or report location).
/// Out-of-range override: if the user is beyond 200m but supplies a reason (≥ 20 chars),
/// check-in is allowed and flagged (Participant.IsCheckInOverridden) for later review.
/// </remarks>
public sealed class CheckInCommunityCleanupCommandHandler(
    ICommunityCleanupEventRepository events,
    ICommunityCleanupParticipantRepository participants,
    IReportRepository reports,
    IGeoDistanceService geoDistance,
    ISystemSettingsProvider systemSettings,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<CheckInCommunityCleanupCommandHandler> logger) : IRequestHandler<CheckInCommunityCleanupCommand, Result>
{
    public async Task<Result> Handle(CheckInCommunityCleanupCommand request, CancellationToken ct)
    {
        var maxCheckInDistanceMeters = ModuleSystemSettings.CheckInMaxDistanceMeters(systemSettings);

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

        var isOutOfRange = distance > maxCheckInDistanceMeters;
        if (isOutOfRange && string.IsNullOrWhiteSpace(request.Reason))
        {
            logger.LogWarning(
                "Check-in distance {Distance}m exceeds max {Max}m for event {EventId} (setting: check_in_max_distance_meters)",
                distance,
                maxCheckInDistanceMeters,
                request.EventId);
            return Errors.CommunityCleanup.CheckInTooFar(distance);
        }

        try
        {
            participant.CheckIn(request.Latitude, request.Longitude, isOutOfRange ? request.Reason : null);
        }
        catch (InvalidOperationException)
        {
            return Errors.CommunityCleanup.InvalidStatusTransition;
        }

        if (participant.Role == CommunityCleanupParticipantRole.Leader)
            ev.AddDomainEvent(new CommunityCleanupLeaderCheckedInEvent(ev.Id, ev.Title, currentUser.UserId, ev.CreatedByLeoId));

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        if (isOutOfRange)
        {
            logger.LogInformation(
                "User {UserId} checked in for community cleanup {EventId} at {Distance:F1}m (out-of-range override, reason: {Reason})",
                currentUser.UserId, request.EventId, distance, request.Reason);
        }
        else
        {
            logger.LogInformation("User {UserId} checked in for community cleanup {EventId} at {Distance:F1}m", currentUser.UserId, request.EventId, distance);
        }

        return Result.Success();
    }
}
