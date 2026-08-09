using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Gamification.EventHandlers;

/// <summary>
/// Listens to Report domain events and triggers gamification point awards.
/// Reads point values from GamificationConfig (BR-ADM-005) — no hardcoded values.
/// Decoupled from Report handlers — zero changes to existing command handlers.
/// </summary>
/// <remarks>Implements: BR-GAM-001, BR-ADM-005.</remarks>
public sealed class ReportVerifiedPointsHandler(
    ISender sender,
    IApplicationDbContext db,
    ILogger<ReportVerifiedPointsHandler> logger)
    : INotificationHandler<ReportVerifiedEvent>
{
    public async Task Handle(ReportVerifiedEvent notification, CancellationToken ct)
    {
        var points = await GamificationPointAwarder
            .GetConfiguredPointsAsync(db, PointReason.ReportVerified, 10, ct)
            .ConfigureAwait(false);

        if (points == 0)
            return;

        await GamificationPointAwarder.TryAwardAsync(
            sender,
            logger,
            notification.ReporterId,
            points,
            PointReason.ReportVerified,
            notification.ReportId,
            "ReportVerified",
            checkBadges: true,
            ct).ConfigureAwait(false);
    }
}

public sealed class ReportResolvedPointsHandler(
    ISender sender,
    IApplicationDbContext db,
    ILogger<ReportResolvedPointsHandler> logger)
    : INotificationHandler<ReportResolvedEvent>
{
    public async Task Handle(ReportResolvedEvent notification, CancellationToken ct)
    {
        var points = await GamificationPointAwarder
            .GetConfiguredPointsAsync(db, PointReason.ReportResolved, 20, ct)
            .ConfigureAwait(false);

        if (points == 0)
            return;

        await GamificationPointAwarder.TryAwardAsync(
            sender,
            logger,
            notification.ReporterId,
            points,
            PointReason.ReportResolved,
            notification.ReportId,
            "ReportResolved",
            checkBadges: true,
            ct).ConfigureAwait(false);
    }
}

public sealed class ReportRejectedPointsHandler(
    ISender sender,
    IApplicationDbContext db,
    ILogger<ReportRejectedPointsHandler> logger)
    : INotificationHandler<ReportRejectedEvent>
{
    public async Task Handle(ReportRejectedEvent notification, CancellationToken ct)
    {
        var points = await GamificationPointAwarder
            .GetConfiguredPointsAsync(db, PointReason.ReportRejected, -5, ct)
            .ConfigureAwait(false);

        if (points == 0)
            return;

        await GamificationPointAwarder.TryAwardAsync(
            sender,
            logger,
            notification.ReporterId,
            points,
            PointReason.ReportRejected,
            notification.ReportId,
            "ReportRejected",
            checkBadges: false,
            ct).ConfigureAwait(false);
    }
}
