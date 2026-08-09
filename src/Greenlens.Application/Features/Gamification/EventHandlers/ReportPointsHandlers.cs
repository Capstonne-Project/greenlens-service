using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
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
    IApplicationDbContext db,
    ILogger<ReportVerifiedPointsHandler> logger)
    : INotificationHandler<ReportVerifiedEvent>
{
    public async Task Handle(ReportVerifiedEvent notification, CancellationToken ct)
    {
        var points = await GamificationPointAwarder
            .GetConfiguredPointsAsync(db, PointReason.ReportVerified, 10, ct)
            .ConfigureAwait(false);

        if (points != 0)
        {
            await GamificationPointAwarder.TryAwardAsync(
                sender,
                logger,
                notification.ReporterId,
                points,
                PointReason.ReportVerified,
                notification.ReportId,
                "ReportVerified",
                checkBadges: false,
                ct).ConfigureAwait(false);
        }

        await GamificationPointAwarder.TryCheckBadgesAsync(sender, notification.ReporterId, ct)
            .ConfigureAwait(false);
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

        if (points != 0)
        {
            await GamificationPointAwarder.TryAwardAsync(
                sender,
                logger,
                notification.ReporterId,
                points,
                PointReason.ReportResolved,
                notification.ReportId,
                "ReportResolved",
                checkBadges: false,
                ct).ConfigureAwait(false);
        }

        await GamificationPointAwarder.TryCheckBadgesAsync(sender, notification.ReporterId, ct)
            .ConfigureAwait(false);
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

/// <summary>
/// Rechecks badge eligibility on every report submission — not just after point-awarding events.
/// Needed because some badges (e.g. streak_7d, streak_30d) progress purely with the passage of
/// calendar days and new submissions, independent of LEO verification/resolution timing. Without
/// this hook, a user who already qualifies (e.g. submitted on 7 consecutive days) would never be
/// awarded the badge until an unrelated point event happened to fire later (or possibly never, if
/// their reports are rejected instead of verified). Awards no points — CheckBadgesCommand only
/// evaluates and persists newly-earned badges.
/// </summary>
/// <remarks>Implements: BR-GAM-004.</remarks>
public sealed class ReportSubmittedBadgeCheckHandler(
    ISender sender,
    IReportRepository reports,
    ILogger<ReportSubmittedBadgeCheckHandler> logger)
    : INotificationHandler<ReportSubmittedEvent>
{
    public async Task Handle(ReportSubmittedEvent notification, CancellationToken ct)
    {
        var reporterId = await reports.QueryAsNoTracking()
            .Where(r => r.Id == notification.ReportId)
            .Select(r => r.ReporterId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (!reporterId.HasValue)
        {
            logger.LogDebug(
                "Badge recheck skipped for report {ReportId}: no reporter",
                notification.ReportId);
            return;
        }

        await sender.Send(new CheckBadgesCommand(reporterId.Value), ct).ConfigureAwait(false);
    }
}
