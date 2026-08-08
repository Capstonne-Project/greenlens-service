using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Gamification;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Gamification.EventHandlers;

/// <summary>
/// Listens to CommunityCleanupCompletedEvent and awards points/badges to every checked-in
/// participant — not just the original reporter (who is separately rewarded via ReportResolvedEvent).
/// </summary>
/// <remarks>Implements: BR-GAM-001, BR-ADM-005.</remarks>
public sealed class CommunityCleanupParticipationPointsHandler(
    ISender sender,
    IApplicationDbContext db,
    ICommunityCleanupParticipantRepository participants,
    ILogger<CommunityCleanupParticipationPointsHandler> logger)
    : INotificationHandler<CommunityCleanupCompletedEvent>
{
    public async Task Handle(CommunityCleanupCompletedEvent notification, CancellationToken ct)
    {
        var points = await GamificationPointAwarder
            .GetConfiguredPointsAsync(db, PointReason.CommunityCleanupParticipation, 15, ct)
            .ConfigureAwait(false);

        if (points == 0)
            return;

        var checkedInParticipants = (await participants
            .GetByEventIdAsync(notification.EventId, ct)
            .ConfigureAwait(false))
            .Where(p => p.Status == CommunityCleanupParticipantStatus.CheckedIn)
            .ToList();

        foreach (var participant in checkedInParticipants)
        {
            // EventId (not ReportId) is the idempotency key — one report can host multiple
            // cleanup events over time (e.g. after a Cancelled one), each must award once.
            await GamificationPointAwarder.TryAwardAsync(
                sender,
                logger,
                participant.UserId,
                points,
                PointReason.CommunityCleanupParticipation,
                notification.EventId,
                "CommunityCleanupParticipation",
                checkBadges: true,
                ct).ConfigureAwait(false);
        }
    }
}
