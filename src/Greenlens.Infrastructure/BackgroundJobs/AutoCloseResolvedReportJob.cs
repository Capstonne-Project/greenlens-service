using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-REP-016: Auto-close reports that have been in Resolved status for ≥ 7 days
/// without citizen confirmation or reopening.
/// Runs hourly. Processes in batches of 100 to avoid long transactions.
/// Creates ReportStatusHistory records and notifies report owner.
/// </summary>
[AutomaticRetry(Attempts = 2)]
internal sealed class AutoCloseResolvedReportJob(
    ApplicationDbContext db,
    ILogger<AutoCloseResolvedReportJob> logger)
{
    private const int BatchSize = 100;
    private static readonly TimeSpan AutoCloseAfter = TimeSpan.FromDays(7);

    public async Task ExecuteAsync()
    {
        logger.LogInformation("AutoCloseResolvedReportJob: Starting...");

        var cutoff = DateTime.UtcNow - AutoCloseAfter;
        var totalClosed = 0;

        while (true)
        {
            var reports = await db.Reports
                .Where(r => r.Status == ReportStatus.Resolved
                         && !r.HasPendingReopenRequest
                         && r.ResolvedAt != null
                         && r.ResolvedAt <= cutoff)
                .OrderBy(r => r.ResolvedAt)
                .Take(BatchSize)
                .ToListAsync()
                .ConfigureAwait(false);

            if (reports.Count == 0)
                break;

            foreach (var report in reports)
            {
                report.Close();

                // Record status history for audit trail
                db.ReportStatusHistory.Add(ReportStatusHistory.Create(
                    report.Id,
                    ReportStatus.Resolved,
                    ReportStatus.Closed,
                    changedBy: null)); // System auto-close

                // BR-REP-016: Notify citizen that report was auto-closed
                if (report.ReporterId.HasValue)
                {
                    db.Notifications.Add(Notification.Create(
                        report.ReporterId.Value,
                        NotificationType.ReportAutoClosed,
                        "Báo cáo đã tự động đóng",
                        $"Báo cáo {report.Code} đã được tự động đóng sau 7 ngày không có phản hồi.",
                        referenceId: report.Id));
                }
            }

            await db.SaveChangesAsync().ConfigureAwait(false);
            totalClosed += reports.Count;

            logger.LogInformation(
                "AutoCloseResolvedReportJob: Closed {Count} reports in this batch",
                reports.Count);

            if (reports.Count < BatchSize)
                break;
        }

        logger.LogInformation(
            "AutoCloseResolvedReportJob: Completed. Total closed: {Total}", totalClosed);
    }
}
