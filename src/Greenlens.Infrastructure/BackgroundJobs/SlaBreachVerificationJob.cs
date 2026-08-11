using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-OFF-002: Flag reports that have been Submitted for > 24 hours
/// without LEO verification. Keeps report with assigned LEO, notifies LEO,
/// and boosts queue priority — no DEO escalation.
/// Runs every 15 minutes.
/// </summary>
/// <remarks>Implements: BR-OFF-002, BR-NTF-002.</remarks>
[AutomaticRetry(Attempts = 2)]
internal sealed class SlaBreachVerificationJob(
    ApplicationDbContext db,
    INotificationService notificationService,
    ILogger<SlaBreachVerificationJob> logger)
{
    private const decimal SlaBreachPriorityBoost = 100m;

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

            var ageHours = (decimal)(now - report.CreatedAt).TotalHours;
            var boostedScore = (int)report.Severity * 3m
                             + report.ReporterCount * 2m
                             + ageHours / 24m
                             + SlaBreachPriorityBoost;
            report.UpdatePriorityScore(Math.Round(boostedScore, 2));
        }

        await db.SaveChangesAsync().ConfigureAwait(false);

        foreach (var report in breachedReports)
        {
            var placeholders = JobNotificationPlaceholders.ForReport(report.Code);
            placeholders = await JobNotificationPlaceholders
                .EnrichFromWardCodeAsync(db, placeholders, report.WardCode)
                .ConfigureAwait(false);

            if (!report.AssignedOfficeId.HasValue)
                continue;

            Guid? leoId = await db.LocalOffices
                .AsNoTracking()
                .Where(o => o.Id == report.AssignedOfficeId)
                .Select(o => o.OfficerId)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (leoId is null || leoId == Guid.Empty)
            {
                leoId = await db.Users
                    .AsNoTracking()
                    .Where(u => u.LocalOfficeId == report.AssignedOfficeId && !u.IsBanned)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
            }

            if (leoId is not null && leoId != Guid.Empty)
            {
                await notificationService.SendFromTemplateAsync(
                    leoId.Value,
                    NotificationType.SlaVerificationBreachedLeo,
                    placeholders,
                    report.Id).ConfigureAwait(false);
            }
        }

        logger.LogWarning(
            "SlaBreachVerificationJob: Flagged {Count} reports with SLA verification breach (LEO retained, priority boosted)",
            breachedReports.Count);
    }
}
