using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications.EventHandlers;

/// <summary>
/// Notifies citizens who have previously reported within 2km of a newly submitted report.
/// </summary>
/// <remarks>Implements: BR-NTF-002 (nearby report), BR-MAP-002 (2km radius).</remarks>
internal sealed class NearbyReportNotificationHandler(
    INotificationService notificationService,
    INearbyCitizenQuery nearbyCitizenQuery,
    IReportRepository reports,
    ISystemSettingsProvider systemSettings,
    IApplicationDbContext db,
    ILogger<NearbyReportNotificationHandler> logger)
    : INotificationHandler<ReportSubmittedEvent>
{
    public async Task Handle(ReportSubmittedEvent notification, CancellationToken ct)
    {
        var (radiusMeters, maxRecipients, _) = ModuleSystemSettings.Notifications(systemSettings);

        var report = await reports.QueryAsNoTracking()
            .Where(r => r.Id == notification.ReportId)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.Latitude,
                r.Longitude,
                r.ReporterId,
                r.WardCode,
                CategoryName = r.Category.NameVi
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (report is null)
        {
            logger.LogWarning(
                "NearbyReport notification skipped: report {ReportId} not found",
                notification.ReportId);
            return;
        }

        var recipientIds = await nearbyCitizenQuery
            .FindCitizenIdsWithinRadiusAsync(
                report.Latitude,
                report.Longitude,
                report.ReporterId,
                radiusMeters,
                maxRecipients,
                ct)
            .ConfigureAwait(false);

        if (recipientIds.Count == 0)
        {
            logger.LogDebug(
                "NearbyReport: no citizens within {Radius}m of report {ReportCode}",
                radiusMeters, report.Code);
            return;
        }

        var categoryName = string.IsNullOrWhiteSpace(report.CategoryName)
            ? "Ô nhiễm môi trường"
            : report.CategoryName;

        var placeholders = NotificationPlaceholders.ForNearbyReport(report.Code, categoryName);
        placeholders = await NotificationLocalityQueries
            .EnrichFromWardCodeAsync(db, placeholders, report.WardCode, ct)
            .ConfigureAwait(false);

        foreach (var recipientId in recipientIds)
        {
            await notificationService.SendFromTemplateAsync(
                recipientId,
                NotificationType.NearbyReport,
                placeholders,
                report.Id,
                ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "NearbyReport: notified {Count} citizen(s) about report {ReportCode}",
            recipientIds.Count, report.Code);
    }
}
