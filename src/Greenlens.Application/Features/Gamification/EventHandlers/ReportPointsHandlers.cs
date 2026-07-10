using Greenlens.Application.Features.Gamification.AwardPoints;
using Greenlens.Application.Features.Gamification.CheckBadges;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
    DbContext db,
    ILogger<ReportVerifiedPointsHandler> logger)
    : INotificationHandler<ReportVerifiedEvent>
{
    public async Task Handle(ReportVerifiedEvent notification, CancellationToken ct)
    {
        var points = await GetConfiguredPointsAsync(PointReason.ReportVerified, 10, ct)
            .ConfigureAwait(false);
        if (points == 0) return; // BR-ADM-005: action disabled

        logger.LogDebug("Gamification: ReportVerified → {Points} points for user {UserId}",
            points, notification.ReporterId);

        await sender.Send(new AwardPointsCommand(
            notification.ReporterId, points, PointReason.ReportVerified, notification.ReportId), ct)
            .ConfigureAwait(false);

        // Check badges after awarding points
        await sender.Send(new CheckBadgesCommand(notification.ReporterId), ct)
            .ConfigureAwait(false);
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

public sealed class ReportResolvedPointsHandler(
    ISender sender,
    DbContext db,
    ILogger<ReportResolvedPointsHandler> logger)
    : INotificationHandler<ReportResolvedEvent>
{
    public async Task Handle(ReportResolvedEvent notification, CancellationToken ct)
    {
        var points = await GetConfiguredPointsAsync(PointReason.ReportResolved, 20, ct)
            .ConfigureAwait(false);
        if (points == 0) return;

        logger.LogDebug("Gamification: ReportResolved → {Points} points for user {UserId}",
            points, notification.ReporterId);

        await sender.Send(new AwardPointsCommand(
            notification.ReporterId, points, PointReason.ReportResolved, notification.ReportId), ct)
            .ConfigureAwait(false);

        await sender.Send(new CheckBadgesCommand(notification.ReporterId), ct)
            .ConfigureAwait(false);
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

public sealed class ReportRejectedPointsHandler(
    ISender sender,
    DbContext db,
    ILogger<ReportRejectedPointsHandler> logger)
    : INotificationHandler<ReportRejectedEvent>
{
    public async Task Handle(ReportRejectedEvent notification, CancellationToken ct)
    {
        var points = await GetConfiguredPointsAsync(PointReason.ReportRejected, -5, ct)
            .ConfigureAwait(false);
        if (points == 0) return;

        logger.LogDebug("Gamification: ReportRejected → {Points} points for user {UserId}",
            points, notification.ReporterId);

        await sender.Send(new AwardPointsCommand(
            notification.ReporterId, points, PointReason.ReportRejected, notification.ReportId), ct)
            .ConfigureAwait(false);
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
