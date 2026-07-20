using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-OFF-020: Flag reports that have been InProgress beyond their severity-based
/// resolution SLA deadline. SLA durations: Critical=3d, High=5d, Medium=7d, Low=10d.
/// Runs every 30 minutes.
/// </summary>
[AutomaticRetry(Attempts = 2)]
internal sealed class SlaBreachResolutionJob(
    ApplicationDbContext db,
    ILogger<SlaBreachResolutionJob> logger)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("SlaBreachResolutionJob: Starting...");

        var now = DateTime.UtcNow;

        var breachedReports = await db.Reports
            .Where(r => r.Status == ReportStatus.InProgress
                     && r.SlaResolveDueAt != null
                     && r.SlaResolveDueAt <= now
                     && !r.SlaResolveBreached)
            .ToListAsync()
            .ConfigureAwait(false);

        if (breachedReports.Count == 0)
        {
            logger.LogInformation("SlaBreachResolutionJob: No breaches found.");
            return;
        }

        foreach (var report in breachedReports)
        {
            report.MarkSlaResolveBreached();

            // BR-OFF-020: Notify LEO
            if (report.AssignedOfficeId.HasValue)
            {
                var leoId = await db.Users
                    .Where(u => u.LocalOfficeId == report.AssignedOfficeId && !u.IsBanned)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);

                if (leoId != Guid.Empty)
                {
                    db.Notifications.Add(Notification.Create(
                        leoId,
                        NotificationType.SlaBreachWarning,
                        "Vượt SLA xử lý",
                        $"Báo cáo {report.Code} ({report.Severity}) đã vượt SLA xử lý. Vui lòng kiểm tra.",
                        referenceId: report.Id));
                }
            }
        }

        await db.SaveChangesAsync().ConfigureAwait(false);

        logger.LogWarning(
            "SlaBreachResolutionJob: Flagged {Count} reports with SLA resolution breach",
            breachedReports.Count);
    }
}
