using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications.EventHandlers;

/// <summary>
/// Subscribes to Report domain events and sends notifications to reporters.
/// Decoupled — zero changes to existing Report handlers.
/// </summary>
/// <remarks>Implements: BR-NTF-002 (report status change triggers notification).</remarks>
internal sealed class ReportVerifiedNotificationHandler(
    INotificationService notificationService,
    ILogger<ReportVerifiedNotificationHandler> logger)
    : INotificationHandler<ReportVerifiedEvent>
{
    public async Task Handle(ReportVerifiedEvent notification, CancellationToken ct)
    {
        logger.LogDebug("Notification: Report {ReportId} verified → notify reporter {UserId}",
            notification.ReportId, notification.ReporterId);

        await notificationService.SendFromTemplateAsync(
            notification.ReporterId,
            NotificationType.ReportStatusChanged,
            new Dictionary<string, string>
            {
                ["report_id"] = notification.ReportId.ToString(),
                ["status"] = "Verified"
            },
            notification.ReportId,
            ct).ConfigureAwait(false);
    }
}

internal sealed class ReportRejectedNotificationHandler(
    INotificationService notificationService,
    ILogger<ReportRejectedNotificationHandler> logger)
    : INotificationHandler<ReportRejectedEvent>
{
    public async Task Handle(ReportRejectedEvent notification, CancellationToken ct)
    {
        logger.LogDebug("Notification: Report {ReportId} rejected → notify reporter {UserId}",
            notification.ReportId, notification.ReporterId);

        await notificationService.SendFromTemplateAsync(
            notification.ReporterId,
            NotificationType.ReportStatusChanged,
            new Dictionary<string, string>
            {
                ["report_id"] = notification.ReportId.ToString(),
                ["status"] = "Rejected"
            },
            notification.ReportId,
            ct).ConfigureAwait(false);
    }
}

internal sealed class ReportResolvedNotificationHandler(
    INotificationService notificationService,
    ILogger<ReportResolvedNotificationHandler> logger)
    : INotificationHandler<ReportResolvedEvent>
{
    public async Task Handle(ReportResolvedEvent notification, CancellationToken ct)
    {
        logger.LogDebug("Notification: Report {ReportId} resolved → notify reporter {UserId}",
            notification.ReportId, notification.ReporterId);

        await notificationService.SendFromTemplateAsync(
            notification.ReporterId,
            NotificationType.ReportStatusChanged,
            new Dictionary<string, string>
            {
                ["report_id"] = notification.ReportId.ToString(),
                ["status"] = "Resolved"
            },
            notification.ReportId,
            ct).ConfigureAwait(false);
    }
}
