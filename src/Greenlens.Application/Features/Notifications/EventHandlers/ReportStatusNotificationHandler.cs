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
/// Notifies LEO/DEO when a new report awaits verification.
/// Skips when the report is already flagged for duplicate or violation recurrence review.
/// </summary>
/// <remarks>
/// Implements: BR-NTF-002 (officer alerts), BR-REP-030 (duplicate queue),
/// BR-REP-034 (violation recurrence queue).
/// </remarks>
internal sealed class ReportSubmittedNotificationHandler(
    INotificationService notificationService,
    IReportRepository reports,
    IApplicationDbContext db,
    ILogger<ReportSubmittedNotificationHandler> logger)
    : INotificationHandler<ReportSubmittedEvent>
{
    public async Task Handle(ReportSubmittedEvent notification, CancellationToken ct)
    {
        var report = await reports.QueryAsNoTracking()
            .Where(r => r.Id == notification.ReportId)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.WardCode,
                r.AssignedOfficeId,
                r.AssignedDepartmentId,
                r.IsPossibleDuplicate,
                r.IsSuspectedViolationRecurrence
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (report is null)
        {
            logger.LogWarning("ReportSubmitted notification skipped: report {ReportId} not found",
                notification.ReportId);
            return;
        }

        if (report.IsPossibleDuplicate)
        {
            logger.LogInformation(
                "ReportSubmitted verification notification skipped: report {ReportCode} flagged as possible duplicate",
                report.Code);
            return;
        }

        if (report.IsSuspectedViolationRecurrence)
        {
            logger.LogInformation(
                "ReportSubmitted verification notification skipped: report {ReportCode} flagged as suspected violation recurrence",
                report.Code);
            return;
        }

        var recipientIds = await ResolveVerifierIdsAsync(
                report.AssignedOfficeId,
                report.AssignedDepartmentId,
                ct)
            .ConfigureAwait(false);

        if (recipientIds.Count == 0)
        {
            logger.LogWarning(
                "ReportSubmitted notification skipped: no LEO/DEO for report {ReportCode}",
                report.Code);
            return;
        }

        var placeholders = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["report_code"] = report.Code
        };
        placeholders = await NotificationLocalityQueries
            .EnrichFromWardCodeAsync(db, placeholders, report.WardCode, ct)
            .ConfigureAwait(false);

        foreach (var recipientId in recipientIds)
        {
            await notificationService.SendFromTemplateAsync(
                recipientId,
                NotificationType.ReportVerificationNeeded,
                placeholders,
                report.Id,
                ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Notified {Count} officer(s) about new report {ReportCode} awaiting verification",
            recipientIds.Count, report.Code);
    }

    private async Task<List<Guid>> ResolveVerifierIdsAsync(
        Guid? assignedOfficeId,
        Guid? assignedDepartmentId,
        CancellationToken ct)
    {
        if (assignedOfficeId.HasValue)
        {
            return await db.Set<User>()
                .AsNoTracking()
                .Where(u => u.Role == UserRole.LEO
                            && u.LocalOfficeId == assignedOfficeId
                            && !u.IsBanned)
                .Select(u => u.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        if (assignedDepartmentId.HasValue)
        {
            return await db.Set<User>()
                .AsNoTracking()
                .Where(u => u.Role == UserRole.DEO
                            && u.DepartmentId == assignedDepartmentId
                            && !u.IsBanned)
                .Select(u => u.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        return [];
    }
}

internal sealed class ReportVerifiedNotificationHandler(
    INotificationService notificationService,
    IReportRepository reports,
    ILogger<ReportVerifiedNotificationHandler> logger)
    : INotificationHandler<ReportVerifiedEvent>
{
    public async Task Handle(ReportVerifiedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Getting report verified notification");

        logger.LogDebug("Notification: Report {ReportId} verified → notify reporter {UserId}",
            notification.ReportId, notification.ReporterId);

        var reportCode = await ResolveReportCodeAsync(reports, notification.ReportId, ct)
            .ConfigureAwait(false);

        await notificationService.SendFromTemplateAsync(
            notification.ReporterId,
            NotificationType.ReportStatusChanged,
            NotificationPlaceholders.ForReportStatus(reportCode, ReportStatus.Verified),
            notification.ReportId,
            ct).ConfigureAwait(false);
    }

    internal static async Task<string> ResolveReportCodeAsync(
        IReportRepository reports, Guid reportId, CancellationToken ct)
    {
        var code = await reports.QueryAsNoTracking()
            .Where(r => r.Id == reportId)
            .Select(r => r.Code)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(code) ? "báo cáo" : code;
    }
}

/// <summary>Notifies reporter when LEO rejects their report (Submitted → Rejected).</summary>
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

        var reportCode = await ReportVerifiedNotificationHandler
            .ResolveReportCodeAsync(reports, notification.ReportId, ct)
            .ConfigureAwait(false);

        await notificationService.SendFromTemplateAsync(
            notification.ReporterId,
            NotificationType.ReportStatusChanged,
            NotificationPlaceholders.ForReportStatus(reportCode, ReportStatus.Rejected),
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

        var reportCode = await ReportVerifiedNotificationHandler
            .ResolveReportCodeAsync(reports, notification.ReportId, ct)
            .ConfigureAwait(false);

        await notificationService.SendFromTemplateAsync(
            notification.ReporterId,
            NotificationType.ReportStatusChanged,
            NotificationPlaceholders.ForReportStatus(reportCode, ReportStatus.Resolved),
            notification.ReportId,
            ct).ConfigureAwait(false);
    }
}
