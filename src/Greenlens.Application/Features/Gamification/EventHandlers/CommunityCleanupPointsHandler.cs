using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Gamification.AwardPoints;
using Greenlens.Application.Features.Gamification.CheckBadges;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Gamification.EventHandlers;

/// <summary>
/// Listens to CommunityCleanupCompletedEvent and awards points/badges to every checked-in
/// participant — not just the original reporter (who is separately rewarded via ReportResolvedEvent).
/// </summary>
public sealed class CommunityCleanupParticipationPointsHandler(
    ISender sender,
    IApplicationDbContext db,
    ICommunityCleanupParticipantRepository participants,
    ILogger<CommunityCleanupParticipationPointsHandler> logger)
    : INotificationHandler<CommunityCleanupCompletedEvent>
{
    public async Task Handle(CommunityCleanupCompletedEvent notification, CancellationToken ct)
    {
        var points = await GetConfiguredPointsAsync(PointReason.CommunityCleanupParticipation, 15, ct)
            .ConfigureAwait(false);
        if (points == 0) return;

        var checkedInParticipants = (await participants
            .GetByEventIdAsync(notification.EventId, ct)
            .ConfigureAwait(false))
            .Where(p => p.Status == CommunityCleanupParticipantStatus.CheckedIn)
            .ToList();

        foreach (var participant in checkedInParticipants)
        {
            logger.LogDebug(
                "Gamification: CommunityCleanupParticipation → {Points} points for user {UserId} (event {EventId})",
                points, participant.UserId, notification.EventId);

            // EventId (not ReportId) is the idempotency key — one report can host multiple
            // cleanup events over time (e.g. after a Cancelled one), each must award once.
            await sender.Send(new AwardPointsCommand(
                participant.UserId, points, PointReason.CommunityCleanupParticipation, notification.EventId), ct)
                .ConfigureAwait(false);

            await sender.Send(new CheckBadgesCommand(participant.UserId), ct)
                .ConfigureAwait(false);
        }
    }

    private async Task<int> GetConfiguredPointsAsync(PointReason reason, int fallback, CancellationToken ct)
    {
        var config = await db.Set<GamificationConfig>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ActionType == reason, ct)
            .ConfigureAwait(false);
        if (config is null) return fallback;
        return config.IsActive ? config.Points : 0;
    }
}
