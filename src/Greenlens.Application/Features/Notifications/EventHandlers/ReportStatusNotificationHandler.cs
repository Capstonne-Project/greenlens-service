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

        await notificationService.SendAsync(
            notification.ReporterId,
            NotificationType.ReportStatusChanged,
            "Báo cáo đã được xác minh",
            "Báo cáo ô nhiễm của bạn đã được xác minh bởi cán bộ môi trường và đang được xử lý.",
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

        await notificationService.SendAsync(
            notification.ReporterId,
            NotificationType.ReportStatusChanged,
            "Báo cáo bị từ chối",
            "Báo cáo ô nhiễm của bạn đã bị từ chối. Vui lòng kiểm tra lý do và gửi lại nếu cần.",
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

        await notificationService.SendAsync(
            notification.ReporterId,
            NotificationType.ReportStatusChanged,
            "Báo cáo đã được giải quyết",
            "Báo cáo ô nhiễm của bạn đã được giải quyết. Vui lòng xác nhận hoặc mở lại trong 7 ngày.",
            notification.ReportId,
            ct).ConfigureAwait(false);
    }
}
