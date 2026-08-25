using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Gamification;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.DuplicateDetection.EventHandlers;

/// <summary>
/// BR-REP-032: When a duplicate is confirmed and merged into a primary report, award the
/// duplicate reporter +50% of the base report points (ReportVerified), rounded half-up.
/// </summary>
/// <remarks>Implements: BR-REP-032 (+50% điểm báo cáo gốc), BR-GAM-001, BR-ADM-005.</remarks>
internal sealed class DuplicateMergedPointsHandler(
    ISender sender,
    IApplicationDbContext db,
    ISystemSettingsProvider systemSettings,
    ILogger<DuplicateMergedPointsHandler> logger)
    : INotificationHandler<ReportMarkedDuplicateEvent>
{
    public async Task Handle(ReportMarkedDuplicateEvent notification, CancellationToken ct)
    {
        // Optional kill-switch: Admin can disable DuplicateReport points (BR-ADM-005).
        var duplicateCfg = await db.Set<GamificationConfig>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ActionType == PointReason.DuplicateReport, ct)
            .ConfigureAwait(false);

        var pointsEnabled = duplicateCfg is null || duplicateCfg.IsActive;

        if (pointsEnabled)
        {
            var verifiedCfg = await db.Set<GamificationConfig>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ActionType == PointReason.ReportVerified, ct)
                .ConfigureAwait(false);

            var basePoints = verifiedCfg is { IsActive: true } ? verifiedCfg.Points : 10;
            var ratio = ReportSystemSettings.DuplicateMergePointsRatio(systemSettings);
            var points = basePoints > 0
                ? (int)Math.Round(basePoints * ratio, MidpointRounding.AwayFromZero)
                : 0;

            if (points > 0)
            {
                logger.LogDebug(
                    "Gamification: DuplicateMerged → {Points} points (50% of ReportVerified={Base}) for user {UserId}",
                    points, basePoints, notification.ReporterId);

                await GamificationPointAwarder.TryAwardAsync(
                    sender,
                    logger,
                    notification.ReporterId,
                    points,
                    PointReason.DuplicateReport,
                    notification.PrimaryReportId,
                    "DuplicateReport",
                    checkBadges: false,
                    ct).ConfigureAwait(false);
            }
        }

        await GamificationPointAwarder.TryCheckBadgesAsync(sender, notification.ReporterId, ct)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// BR-REP-032: Notify the duplicate reporter that their report was merged into an existing one.
/// </summary>
/// <remarks>Implements: BR-REP-032, BR-NTF-002.</remarks>
internal sealed class DuplicateMergedNotificationHandler(
    INotificationService notificationService,
    IReportRepository reports,
    ILogger<DuplicateMergedNotificationHandler> logger)
    : INotificationHandler<ReportMarkedDuplicateEvent>
{
    public async Task Handle(ReportMarkedDuplicateEvent notification, CancellationToken ct)
    {
        // Fetch primary report code for notification message
        var primaryCode = await reports.QueryAsNoTracking()
            .Where(r => r.Id == notification.PrimaryReportId)
            .Select(r => r.Code)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        logger.LogDebug(
            "Notification: Report {ReportId} merged as duplicate of {PrimaryCode} → notify reporter {UserId}",
            notification.ReportId, primaryCode, notification.ReporterId);

        await notificationService.SendFromTemplateAsync(
            notification.ReporterId,
            NotificationType.ReportDuplicateMerged,
            NotificationPlaceholders.ForDuplicateMerged(primaryCode ?? "hiện có"),
            notification.PrimaryReportId,
            ct).ConfigureAwait(false);
    }
}
