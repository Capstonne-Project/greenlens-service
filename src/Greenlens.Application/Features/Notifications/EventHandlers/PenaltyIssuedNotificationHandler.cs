using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications.EventHandlers;

/// <summary>Notifies the citizen reporter when a penalty decision is issued on their report.</summary>
/// <remarks>Implements: BR-INS-012, BR-NTF-002.</remarks>
internal sealed class PenaltyIssuedNotificationHandler(
    INotificationService notificationService,
    IReportRepository reports,
    ILogger<PenaltyIssuedNotificationHandler> logger)
    : INotificationHandler<PenaltyIssuedEvent>
{
    public async Task Handle(PenaltyIssuedEvent notification, CancellationToken ct)
    {
        var report = await reports.QueryAsNoTracking()
            .Where(r => r.Id == notification.ReportId)
            .Select(r => new { r.Id, r.Code, r.ReporterId })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (report is null)
        {
            logger.LogWarning(
                "PenaltyIssued notification skipped: report {ReportId} not found",
                notification.ReportId);
            return;
        }

        if (!report.ReporterId.HasValue)
        {
            logger.LogDebug(
                "PenaltyIssued notification skipped: anonymous report {ReportId}",
                notification.ReportId);
            return;
        }

        await notificationService.SendFromTemplateAsync(
            report.ReporterId.Value,
            NotificationType.PenaltyIssued,
            NotificationPlaceholders.ForPenaltyIssued(
                report.Code,
                notification.PenaltyAmount,
                notification.DecisionNumber),
            report.Id,
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "PenaltyIssued: notified reporter {UserId} for report {ReportCode}, decision {DecisionNumber}",
            report.ReporterId.Value, report.Code, notification.DecisionNumber);
    }
}
