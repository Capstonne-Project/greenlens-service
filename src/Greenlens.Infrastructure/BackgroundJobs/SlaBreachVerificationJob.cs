using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-OFF-002: Flag reports that have been Submitted for > 24 hours
/// without LEO verification. SLA breach triggers escalation + notification.
/// Runs every 15 minutes.
/// </summary>
/// <remarks>Implements: BR-OFF-002, BR-ORG-014, BR-NTF-002.</remarks>
[AutomaticRetry(Attempts = 2)]
internal sealed class SlaBreachVerificationJob(
    ApplicationDbContext db,
    INotificationService notificationService,
    ILogger<SlaBreachVerificationJob> logger)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("SlaBreachVerificationJob: Starting...");

        var now = DateTime.UtcNow;

        var breachedReports = await db.Reports
            .Where(r => r.Status == ReportStatus.Submitted
                     && r.SlaVerifyDueAt != null
                     && r.SlaVerifyDueAt <= now
                     && !r.SlaVerifyBreached)
            .ToListAsync()
            .ConfigureAwait(false);

        if (breachedReports.Count == 0)
        {
            logger.LogInformation("SlaBreachVerificationJob: No breaches found.");
            return;
        }

        foreach (var report in breachedReports)
        {
            report.MarkSlaVerifyBreached();
            report.EscalateToDepartment();
        }

        await db.SaveChangesAsync().ConfigureAwait(false);

        foreach (var report in breachedReports)
        {
            var placeholders = JobNotificationPlaceholders.ForReport(report.Code);

            if (report.AssignedOfficeId.HasValue)
            {
                var leoId = await db.Users
                    .AsNoTracking()
                    .Where(u => u.LocalOfficeId == report.AssignedOfficeId && !u.IsBanned)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);

                if (leoId != Guid.Empty)
                {
                    await notificationService.SendFromTemplateAsync(
                        leoId,
                        NotificationType.SlaVerificationBreachedLeo,
                        placeholders,
                        report.Id).ConfigureAwait(false);
                }
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
                {
                    await notificationService.SendFromTemplateAsync(
                        deoId,
                        NotificationType.SlaVerificationEscalatedDeo,
                        placeholders,
                        report.Id).ConfigureAwait(false);
                }
            }
        }

        logger.LogWarning(
            "SlaBreachVerificationJob: Flagged {Count} reports with SLA verification breach",
            breachedReports.Count);
    }
}
