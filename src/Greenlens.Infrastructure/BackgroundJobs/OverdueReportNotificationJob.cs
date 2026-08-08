using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-REP-008: Flag reports pending > 72h as Overdue and notify LEO/DEO.
/// BR-REP-009: Notify LEO when Verified reports are unassigned > 24h.
/// Runs hourly. Idempotent — only flags/notifies once per report.
/// </summary>
/// <remarks>Implements: BR-REP-008, BR-REP-009, BR-NTF-002.</remarks>
[AutomaticRetry(Attempts = 2)]
internal sealed class OverdueReportNotificationJob(
    ApplicationDbContext db,
    INotificationService notificationService,
    ILogger<OverdueReportNotificationJob> logger)
{
    private const int BatchSize = 200;

    public async Task ExecuteAsync()
    {
        logger.LogInformation("OverdueReportNotificationJob: Starting...");

        var overdueCount = await ProcessOverdueReportsAsync().ConfigureAwait(false);
        var unassignedCount = await ProcessUnassignedReportsAsync().ConfigureAwait(false);

        logger.LogInformation(
            "OverdueReportNotificationJob: Completed. Overdue flagged: {Overdue}, Unassigned notified: {Unassigned}",
            overdueCount, unassignedCount);
    }

    private async Task<int> ProcessOverdueReportsAsync()
    {
        var cutoff = DateTime.UtcNow.AddHours(-72);

        var reports = await db.Reports
            .Where(r => (r.Status == ReportStatus.Submitted || r.Status == ReportStatus.Verified)
                        && r.CreatedAt <= cutoff
                        && !r.IsOverdue)
            .OrderBy(r => r.Id)
            .Take(BatchSize)
            .ToListAsync()
            .ConfigureAwait(false);

        if (reports.Count == 0)
            return 0;

        foreach (var report in reports)
            report.MarkOverdue();

        await db.SaveChangesAsync().ConfigureAwait(false);

        foreach (var report in reports)
        {
            var recipientId = await ResolveOfficerIdAsync(report).ConfigureAwait(false);
            if (!recipientId.HasValue)
                continue;

            var placeholders = JobNotificationPlaceholders.ForReport(report.Code);
            placeholders = await JobNotificationPlaceholders
                .EnrichFromWardCodeAsync(db, placeholders, report.WardCode)
                .ConfigureAwait(false);

            await notificationService.SendFromTemplateAsync(
                recipientId.Value,
                NotificationType.ReportOverdue,
                placeholders,
                report.Id).ConfigureAwait(false);
        }

        logger.LogWarning(
            "OverdueReportNotificationJob: Flagged {Count} reports as overdue (BR-REP-008)",
            reports.Count);

        return reports.Count;
    }

    private async Task<int> ProcessUnassignedReportsAsync()
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);

        var reports = await db.Reports
            .Where(r => r.Status == ReportStatus.Verified
                        && r.VerifiedAt != null
                        && r.VerifiedAt <= cutoff
                        && r.AssignedByOfficerId == null)
            .OrderBy(r => r.Id)
            .Take(BatchSize)
            .ToListAsync()
            .ConfigureAwait(false);

        if (reports.Count == 0)
            return 0;

        var reportIds = reports.Select(r => r.Id).ToList();
        var recentlyNotified = await db.Notifications
            .AsNoTracking()
            .Where(n => reportIds.Contains(n.ReferenceId!.Value)
                        && n.Type == NotificationType.ReportUnassigned
                        && n.CreatedAt >= cutoff)
            .Select(n => n.ReferenceId!.Value)
            .Distinct()
            .ToListAsync()
            .ConfigureAwait(false);

        var recentlyNotifiedSet = recentlyNotified.ToHashSet();
        var notified = 0;

        foreach (var report in reports)
        {
            if (recentlyNotifiedSet.Contains(report.Id))
                continue;

            var recipientId = await ResolveOfficerIdAsync(report).ConfigureAwait(false);
            if (!recipientId.HasValue)
                continue;

            var placeholders = JobNotificationPlaceholders.ForReport(report.Code);
            placeholders = await JobNotificationPlaceholders
                .EnrichFromWardCodeAsync(db, placeholders, report.WardCode)
                .ConfigureAwait(false);

            await notificationService.SendFromTemplateAsync(
                recipientId.Value,
                NotificationType.ReportUnassigned,
                placeholders,
                report.Id).ConfigureAwait(false);
            notified++;
        }

        if (notified > 0)
        {
            logger.LogWarning(
                "OverdueReportNotificationJob: Notified {Count} unassigned reports (BR-REP-009)",
                notified);
        }

        return notified;
    }

    private async Task<Guid?> ResolveOfficerIdAsync(Report report)
    {
        if (report.AssignedOfficeId.HasValue)
        {
            var leoId = await db.Users
                .AsNoTracking()
                .Where(u => u.LocalOfficeId == report.AssignedOfficeId && !u.IsBanned)
                .Select(u => u.Id)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (leoId != Guid.Empty)
                return leoId;
        }

        if (report.AssignedDepartmentId.HasValue)
        {
            var deoId = await db.Users
                .AsNoTracking()
                .Where(u => u.DepartmentId == report.AssignedDepartmentId && !u.IsBanned)
                .Select(u => u.Id)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (deoId != Guid.Empty)
                return deoId;
        }

        return null;
    }
}
