using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-OFF-002: Flag reports that have been Submitted for > 24 hours
/// without LEO verification. SLA breach triggers escalation awareness.
/// Runs every 15 minutes.
/// </summary>
[AutomaticRetry(Attempts = 2)]
internal sealed class SlaBreachVerificationJob(
    ApplicationDbContext db,
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
        }

        await db.SaveChangesAsync().ConfigureAwait(false);

        logger.LogWarning(
            "SlaBreachVerificationJob: Flagged {Count} reports with SLA verification breach",
            breachedReports.Count);
    }
}
