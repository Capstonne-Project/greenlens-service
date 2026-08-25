using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-REP-016: Auto-close reports that have been in Resolved status for ≥ 2 days
/// without citizen confirmation or reopening.
/// Runs hourly. Processes in batches of 100 to avoid long transactions.
/// Creates ReportStatusHistory records and notifies report owner.
/// </summary>
/// <remarks>Implements: BR-REP-016, BR-REP-025, BR-NTF-002.</remarks>
[AutomaticRetry(Attempts = 2)]
internal sealed class AutoCloseResolvedReportJob(
    ApplicationDbContext db,
    INotificationService notificationService,
    ISystemSettingsProvider systemSettings,
    ILogger<AutoCloseResolvedReportJob> logger)
{
    private const int BatchSize = 100;

    public async Task ExecuteAsync()
    {
        logger.LogInformation("AutoCloseResolvedReportJob: Starting...");

        var autoCloseDays = ReportSystemSettings.AutoCloseResolvedDays(systemSettings);
        var cutoff = DateTime.UtcNow - TimeSpan.FromDays(autoCloseDays);
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

            var notifyRecipients = new List<(Guid UserId, Guid ReportId, string ReportCode)>();

            foreach (var report in reports)
            {
                report.Close();

                db.ReportStatusHistory.Add(ReportStatusHistory.Create(
                    report.Id,
                    ReportStatus.Resolved,
                    ReportStatus.Closed,
                    changedBy: null));

                if (report.ReporterId.HasValue)
                {
                    notifyRecipients.Add((report.ReporterId.Value, report.Id, report.Code));
                }
            }

            await db.SaveChangesAsync().ConfigureAwait(false);

            foreach (var (userId, reportId, reportCode) in notifyRecipients)
            {
                await notificationService.SendFromTemplateAsync(
                    userId,
                    NotificationType.ReportAutoClosed,
                    JobNotificationPlaceholders.ForReport(reportCode),
                    reportId).ConfigureAwait(false);
            }

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
