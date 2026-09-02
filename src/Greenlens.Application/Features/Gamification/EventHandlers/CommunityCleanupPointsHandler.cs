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

        // Chỉ thưởng điểm/badge cho Member (citizen tham gia) — Leader được giao phụ trách
        // event, không phải đối tượng của phần thưởng "tham gia dọn dẹp cộng đồng".
        var checkedInParticipants = (await participants
            .GetByEventIdAsync(notification.EventId, ct)
            .ConfigureAwait(false))
            .Where(p => p.Status == CommunityCleanupParticipantStatus.CheckedIn
                && p.Role == CommunityCleanupParticipantRole.Member)
            .ToList();

        if (points == 0)
        {
            foreach (var participant in checkedInParticipants)
            {
                await GamificationPointAwarder.TryCheckBadgesAsync(sender, participant.UserId, ct)
                    .ConfigureAwait(false);
            }

            return;
        }

        foreach (var participant in checkedInParticipants)
        {
            await GamificationPointAwarder.TryAwardAsync(
                sender,
                logger,
                participant.UserId,
                points,
                PointReason.CommunityCleanupParticipation,
                notification.EventId,
                "CommunityCleanupParticipation",
                ct).ConfigureAwait(false);

            await GamificationPointAwarder.TryCheckBadgesAsync(sender, participant.UserId, ct)
                .ConfigureAwait(false);
        }
    }
}
