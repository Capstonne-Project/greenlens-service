using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications;

/// <summary>Notifies citizen reporter when inspection closes with no violation (BR-INS-013).</summary>
/// <remarks>Implements: BR-INS-013, BR-NTF-002.</remarks>
public interface IInspectionClosedNoViolationNotifier
{
    Task NotifyReporterAsync(
        Guid reportId,
        string reportCode,
        Guid? reporterId,
        string reason,
        CancellationToken ct = default);
}

public sealed class InspectionClosedNoViolationNotifier(
    INotificationService notificationService,
    ILogger<InspectionClosedNoViolationNotifier> logger) : IInspectionClosedNoViolationNotifier
{
    public async Task NotifyReporterAsync(
        Guid reportId,
        string reportCode,
        Guid? reporterId,
        string reason,
        CancellationToken ct = default)
    {
        if (!reporterId.HasValue)
        {
            logger.LogDebug(
                "InspectionClosedNoViolation skipped: anonymous report {ReportId}",
                reportId);
            return;
        }

        var placeholders = NotificationPlaceholders.ForInspectionClosedNoViolation(
            reportCode,
            reason.Trim());

        await notificationService.SendFromTemplateAsync(
            reporterId.Value,
            NotificationType.InspectionClosedNoViolation,
            placeholders,
            reportId,
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Notified reporter {UserId} that inspection for report {ReportCode} closed with no violation",
            reporterId.Value, reportCode);
    }
}
