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
        var duplicatePoints = await GamificationPointAwarder
            .GetConfiguredPointsAsync(db, PointReason.DuplicateReport, 5, ct)
            .ConfigureAwait(false);

        // DuplicateReport config IsActive=false → tắt cả luồng (đồng bộ với ReportVerified/Resolved).
        if (duplicatePoints == 0)
        {
            await GamificationPointAwarder.TryCheckBadgesAsync(sender, notification.ReporterId, ct)
                .ConfigureAwait(false);
            return;
        }

        var verifiedPoints = await GamificationPointAwarder
            .GetConfiguredPointsAsync(db, PointReason.ReportVerified, 10, ct)
            .ConfigureAwait(false);

        var ratio = ReportSystemSettings.DuplicateMergePointsRatio(systemSettings);
        var points = verifiedPoints > 0
            ? (int)Math.Round(verifiedPoints * ratio, MidpointRounding.AwayFromZero)
            : 0;

        if (points > 0)
        {
            logger.LogDebug(
                "Gamification: DuplicateMerged → {Points} points (50% of ReportVerified={Base}) for user {UserId}",
                points, verifiedPoints, notification.ReporterId);

            await GamificationPointAwarder.TryAwardAsync(
                sender,
                logger,
                notification.ReporterId,
                points,
                PointReason.DuplicateReport,
                notification.PrimaryReportId,
                "DuplicateReport",
                ct).ConfigureAwait(false);
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
