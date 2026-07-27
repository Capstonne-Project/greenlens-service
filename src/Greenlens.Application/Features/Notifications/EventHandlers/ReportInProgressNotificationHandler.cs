using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications.EventHandlers;

/// <summary>Notify reporter when their report enters InProgress (LEO dispatch or team assign).</summary>
/// <remarks>Implements: BR-NTF-002, BR-OFF-011.</remarks>
internal sealed class ReportInProgressNotificationHandler(
    INotificationService notificationService,
    IReportRepository reports,
    ILogger<ReportInProgressNotificationHandler> logger)
    : INotificationHandler<ReportInProgressEvent>
{
    public async Task Handle(ReportInProgressEvent notification, CancellationToken ct)
    {
        if (notification.ReporterId is null)
        {
            logger.LogDebug(
                "ReportInProgress notification skipped: anonymous report {ReportId}",
                notification.ReportId);
            return;
        }

        var reportCode = await reports.QueryAsNoTracking()
            .Where(r => r.Id == notification.ReportId)
            .Select(r => r.Code)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(reportCode))
        {
            logger.LogWarning(
                "ReportInProgress notification skipped: report {ReportId} not found",
                notification.ReportId);
            return;
        }

        await notificationService.SendFromTemplateAsync(
            notification.ReporterId.Value,
            NotificationType.ReportStatusChanged,
            NotificationPlaceholders.ForReportStatus(reportCode, "InProgress"),
            notification.ReportId,
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Notified reporter {UserId} that report {ReportCode} is InProgress",
            notification.ReporterId.Value, reportCode);
    }
}
