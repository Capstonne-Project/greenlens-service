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
/// Notifies LEO/DEO when a new report is flagged as suspected violation recurrence (BR-REP-034).
/// </summary>
/// <remarks>Implements: BR-REP-034, BR-NTF-002.</remarks>
internal sealed class ViolationRecurrenceSuspectedNotificationHandler(
    INotificationService notificationService,
    IReportRepository reports,
    IApplicationDbContext db,
    ILogger<ViolationRecurrenceSuspectedNotificationHandler> logger)
    : INotificationHandler<ReportViolationRecurrenceSuspectedEvent>
{
    public async Task Handle(ReportViolationRecurrenceSuspectedEvent notification, CancellationToken ct)
    {
        var report = await reports.QueryAsNoTracking()
            .Where(r => r.Id == notification.ReportId)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.WardCode,
                r.AssignedOfficeId,
                r.AssignedDepartmentId
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (report is null)
        {
            logger.LogWarning(
                "ViolationRecurrenceSuspected notification skipped: report {ReportId} not found",
                notification.ReportId);
            return;
        }

        var priorCode = await reports.QueryAsNoTracking()
            .Where(r => r.Id == notification.PriorClosedReportId)
            .Select(r => r.Code)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var recipientIds = await ResolveOfficerIdsAsync(
                report.AssignedOfficeId,
                report.AssignedDepartmentId,
                ct)
            .ConfigureAwait(false);

        if (recipientIds.Count == 0)
        {
            logger.LogWarning(
                "ViolationRecurrenceSuspected notification skipped: no LEO/DEO for report {ReportCode}",
                report.Code);
            return;
        }

        var placeholders = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["report_code"] = report.Code,
            ["prior_report_code"] = priorCode ?? "báo cáo đã đóng"
        };
        placeholders = await NotificationLocalityQueries
            .EnrichFromWardCodeAsync(db, placeholders, report.WardCode, ct)
            .ConfigureAwait(false);

        foreach (var recipientId in recipientIds)
        {
            await notificationService.SendFromTemplateAsync(
                recipientId,
                NotificationType.ViolationRecurrenceReviewNeeded,
                placeholders,
                report.Id,
                ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Notified {Count} officer(s) about violation recurrence suspicion on report {ReportCode}",
            recipientIds.Count, report.Code);
    }

    private async Task<List<Guid>> ResolveOfficerIdsAsync(
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
