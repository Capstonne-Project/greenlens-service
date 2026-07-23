using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications.EventHandlers;

/// <summary>
/// Subscribes to Report domain events and sends notifications to reporters.
/// Decoupled — zero changes to existing Report handlers.
/// </summary>
/// <remarks>Implements: BR-NTF-002 (report status change triggers notification).</remarks>
internal sealed class ReportVerifiedNotificationHandler(
    INotificationService notificationService,
    IReportRepository reports,
    ILogger<ReportVerifiedNotificationHandler> logger)
    : INotificationHandler<ReportVerifiedEvent>
{
    public async Task Handle(ReportVerifiedEvent notification, CancellationToken ct)
    {
        logger.LogDebug("Notification: Report {ReportId} verified → notify reporter {UserId}",
            notification.ReportId, notification.ReporterId);

        var categoryName = await ResolveCategoryNameAsync(reports, notification.ReportId, ct)
            .ConfigureAwait(false);

        await notificationService.SendFromTemplateAsync(
            notification.ReporterId,
            NotificationType.ReportStatusChanged,
            new Dictionary<string, string>
            {
                ["report_id"] = categoryName,
                ["status"] = "Verified"
            },
            notification.ReportId,
            ct).ConfigureAwait(false);
    }

    internal static async Task<string> ResolveCategoryNameAsync(
        IReportRepository reports, Guid reportId, CancellationToken ct)
    {
        var name = await reports.QueryAsNoTracking()
            .Where(r => r.Id == reportId)
            .Select(r => r.Category.NameVi)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(name) ? "Báo cáo ô nhiễm" : name;
    }
}

internal sealed class ReportRejectedNotificationHandler(
    INotificationService notificationService,
    IReportRepository reports,
    ILogger<ReportRejectedNotificationHandler> logger)
    : INotificationHandler<ReportRejectedEvent>
{
    public async Task Handle(ReportRejectedEvent notification, CancellationToken ct)
    {
        logger.LogDebug("Notification: Report {ReportId} rejected → notify reporter {UserId}",
            notification.ReportId, notification.ReporterId);

        var categoryName = await ReportVerifiedNotificationHandler
            .ResolveCategoryNameAsync(reports, notification.ReportId, ct)
            .ConfigureAwait(false);

        await notificationService.SendFromTemplateAsync(
            notification.ReporterId,
            NotificationType.ReportStatusChanged,
            new Dictionary<string, string>
            {
                ["report_id"] = categoryName,
                ["status"] = "Rejected"
            },
            notification.ReportId,
            ct).ConfigureAwait(false);
    }
}

internal sealed class ReportResolvedNotificationHandler(
    INotificationService notificationService,
    IReportRepository reports,
    ILogger<ReportResolvedNotificationHandler> logger)
    : INotificationHandler<ReportResolvedEvent>
{
    public async Task Handle(ReportResolvedEvent notification, CancellationToken ct)
    {
        logger.LogDebug("Notification: Report {ReportId} resolved → notify reporter {UserId}",
            notification.ReportId, notification.ReporterId);

        var categoryName = await ReportVerifiedNotificationHandler
            .ResolveCategoryNameAsync(reports, notification.ReportId, ct)
            .ConfigureAwait(false);

        await notificationService.SendFromTemplateAsync(
            notification.ReporterId,
            NotificationType.ReportStatusChanged,
            new Dictionary<string, string>
            {
                ["report_id"] = categoryName,
                ["status"] = "Resolved"
            },
            notification.ReportId,
            ct).ConfigureAwait(false);
    }
}
