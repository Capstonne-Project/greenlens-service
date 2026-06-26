using Greenlens.Application.Features.Gamification.AwardPoints;
using Greenlens.Application.Features.Gamification.CheckBadges;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Gamification.EventHandlers;

/// <summary>
/// Listens to Report domain events and triggers gamification point awards.
/// Decoupled from Report handlers — zero changes to existing command handlers.
/// </summary>
/// <remarks>Implements: BR-GAM-001.</remarks>
public sealed class ReportVerifiedPointsHandler(
    ISender sender,
    ILogger<ReportVerifiedPointsHandler> logger)
    : INotificationHandler<ReportVerifiedEvent>
{
    public async Task Handle(ReportVerifiedEvent notification, CancellationToken ct)
    {
        logger.LogDebug("Gamification: ReportVerified → +10 points for user {UserId}", notification.ReporterId);

        await sender.Send(new AwardPointsCommand(
            notification.ReporterId, Points: 10, PointReason.ReportVerified, notification.ReportId), ct)
            .ConfigureAwait(false);

        // Check badges after awarding points
        await sender.Send(new CheckBadgesCommand(notification.ReporterId), ct)
            .ConfigureAwait(false);
    }
}

public sealed class ReportResolvedPointsHandler(
    ISender sender,
    ILogger<ReportResolvedPointsHandler> logger)
    : INotificationHandler<ReportResolvedEvent>
{
    public async Task Handle(ReportResolvedEvent notification, CancellationToken ct)
    {
        logger.LogDebug("Gamification: ReportResolved → +20 points for user {UserId}", notification.ReporterId);

        await sender.Send(new AwardPointsCommand(
            notification.ReporterId, Points: 20, PointReason.ReportResolved, notification.ReportId), ct)
            .ConfigureAwait(false);

        await sender.Send(new CheckBadgesCommand(notification.ReporterId), ct)
            .ConfigureAwait(false);
    }
}

public sealed class ReportRejectedPointsHandler(
    ISender sender,
    ILogger<ReportRejectedPointsHandler> logger)
    : INotificationHandler<ReportRejectedEvent>
{
    public async Task Handle(ReportRejectedEvent notification, CancellationToken ct)
    {
        logger.LogDebug("Gamification: ReportRejected → -5 points for user {UserId}", notification.ReporterId);

        await sender.Send(new AwardPointsCommand(
            notification.ReporterId, Points: -5, PointReason.ReportRejected, notification.ReportId), ct)
            .ConfigureAwait(false);
    }
}
