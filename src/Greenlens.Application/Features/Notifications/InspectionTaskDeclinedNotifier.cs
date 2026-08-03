using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications;

/// <summary>Notifies LEO when an inspection team declines a task (BR-INS-003).</summary>
/// <remarks>Implements: BR-INS-003, BR-NTF-002.</remarks>
public interface IInspectionTaskDeclinedNotifier
{
    Task NotifyLeoAsync(
        Guid leoUserId,
        Guid reportId,
        string reportCode,
        string declineReason,
        CancellationToken ct = default);
}

public sealed class InspectionTaskDeclinedNotifier(
    INotificationService notificationService,
    IApplicationDbContext db,
    ILogger<InspectionTaskDeclinedNotifier> logger) : IInspectionTaskDeclinedNotifier
{
    public async Task NotifyLeoAsync(
        Guid leoUserId,
        Guid reportId,
        string reportCode,
        string declineReason,
        CancellationToken ct = default)
    {
        var placeholders = NotificationPlaceholders.ForInspectionTaskDeclined(
            reportCode,
            declineReason.Trim());
        placeholders = await NotificationLocalityQueries
            .EnrichFromReportIdAsync(db, placeholders, reportId, ct)
            .ConfigureAwait(false);

        await notificationService.SendFromTemplateAsync(
            leoUserId,
            NotificationType.InspectionTaskDeclined,
            placeholders,
            reportId,
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Notified LEO {UserId} that inspection task for report {ReportCode} was declined",
            leoUserId, reportCode);
    }
}
