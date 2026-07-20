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
[AutomaticRetry(Attempts = 2)]
internal sealed class OverdueReportNotificationJob(
    ApplicationDbContext db,
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

    /// <summary>
    /// BR-REP-008: Reports at Submitted/Verified for > 72h → set IsOverdue + notify.
    /// </summary>
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
        {
            report.MarkOverdue();

            // Notify the assigned LEO or DEO
            var recipientId = await ResolveOfficerIdAsync(report).ConfigureAwait(false);
            if (recipientId.HasValue)
            {
                db.Notifications.Add(Notification.Create(
                    recipientId.Value,
                    NotificationType.ReportOverdue,
                    "Báo cáo tồn đọng quá 72 giờ",
                    $"Báo cáo {report.Code} đã chờ xử lý hơn 72 giờ. Vui lòng kiểm tra.",
                    referenceId: report.Id));
            }
        }

        await db.SaveChangesAsync().ConfigureAwait(false);

        logger.LogWarning(
            "OverdueReportNotificationJob: Flagged {Count} reports as overdue (BR-REP-008)",
            reports.Count);

        return reports.Count;
    }

    /// <summary>
    /// BR-REP-009: Verified reports with no team assignment > 24h → notify LEO.
    /// </summary>
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

        // Deduplicate: skip reports that already got this notification in the last 24h
        var reportIds = reports.Select(r => r.Id).ToList();
        var recentlyNotified = await db.Notifications
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
            if (recipientId.HasValue)
            {
                db.Notifications.Add(Notification.Create(
                    recipientId.Value,
                    NotificationType.ReportUnassigned,
                    "Báo cáo chưa phân công quá 24 giờ",
                    $"Báo cáo {report.Code} đã xác minh nhưng chưa gán đội xử lý sau 24 giờ.",
                    referenceId: report.Id));
                notified++;
            }
        }

        if (notified > 0)
        {
            await db.SaveChangesAsync().ConfigureAwait(false);
            logger.LogWarning(
                "OverdueReportNotificationJob: Notified {Count} unassigned reports (BR-REP-009)",
                notified);
        }

        return notified;
    }

    /// <summary>
    /// Resolve the LEO or DEO responsible for a report.
    /// Prefers LEO (LocalOffice head) → falls back to DEO (Department head).
    /// </summary>
    private async Task<Guid?> ResolveOfficerIdAsync(Report report)
    {
        // Try LEO: find any staff member in the assigned LocalOffice
        if (report.AssignedOfficeId.HasValue)
        {
            var leoId = await db.Users
                .Where(u => u.LocalOfficeId == report.AssignedOfficeId && !u.IsBanned)
                .Select(u => u.Id)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (leoId != Guid.Empty)
                return leoId;
        }

        // Fallback to DEO: find any staff in the assigned Department
        if (report.AssignedDepartmentId.HasValue)
        {
            var deoId = await db.Users
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
