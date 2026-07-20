using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Gamification.AwardPoints;
using Greenlens.Application.Features.Gamification.CheckBadges;
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
    ILogger<DuplicateMergedPointsHandler> logger)
    : INotificationHandler<ReportMarkedDuplicateEvent>
{
    public async Task Handle(ReportMarkedDuplicateEvent notification, CancellationToken ct)
    {
        // Optional kill-switch: Admin can disable DuplicateReport in GamificationConfig (BR-ADM-005).
        var duplicateCfg = await db.Set<GamificationConfig>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ActionType == PointReason.DuplicateReport, ct)
            .ConfigureAwait(false);
        if (duplicateCfg is not null && !duplicateCfg.IsActive)
            return;

        var verifiedCfg = await db.Set<GamificationConfig>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ActionType == PointReason.ReportVerified, ct)
            .ConfigureAwait(false);

        // Base = ReportVerified points (fallback 10). Award = round half-up of 50%.
        var basePoints = verifiedCfg is { IsActive: true } ? verifiedCfg.Points : 10;
        if (basePoints <= 0)
            return;

        var points = (int)Math.Round(basePoints * 0.5, MidpointRounding.AwayFromZero);
        if (points == 0)
            return;

        logger.LogDebug(
            "Gamification: DuplicateMerged → {Points} points (50% of ReportVerified={Base}) for user {UserId}",
            points, basePoints, notification.ReporterId);

        await sender.Send(new AwardPointsCommand(
            notification.ReporterId, points, PointReason.DuplicateReport, notification.PrimaryReportId), ct)
            .ConfigureAwait(false);

        await sender.Send(new CheckBadgesCommand(notification.ReporterId), ct)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// BR-REP-032: Notify the duplicate reporter that their report was merged into an existing one.
/// </summary>
/// <remarks>Implements: BR-REP-032, BR-NTF-002.</remarks>
internal sealed class DuplicateMergedNotificationHandler(
    INotificationService notificationService,
    ILogger<DuplicateMergedNotificationHandler> logger)
    : INotificationHandler<ReportMarkedDuplicateEvent>
{
    public async Task Handle(ReportMarkedDuplicateEvent notification, CancellationToken ct)
    {
        logger.LogDebug(
            "Notification: Report {ReportId} merged as duplicate → notify reporter {UserId}",
            notification.ReportId, notification.ReporterId);

        await notificationService.SendRawAsync(
            notification.ReporterId,
            NotificationType.ReportStatusChanged,
            "Báo cáo được gộp",
            "Báo cáo của bạn đã được xác định là trùng lặp và gộp vào một báo cáo hiện có. Cảm ơn đóng góp của bạn!",
            notification.ReportId,
            ct).ConfigureAwait(false);
    }
}
