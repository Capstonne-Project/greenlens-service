using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-OFF-010: Recalculate PriorityScore for active reports.
/// Formula: severity * 3 + reporterCount * 2 + ageInHours / 24.
/// Runs every 30 minutes. Batch 200.
/// </summary>
[AutomaticRetry(Attempts = 2)]
internal sealed class PriorityScoreRefreshJob(
    ApplicationDbContext db,
    ISystemSettingsProvider systemSettings,
    ILogger<PriorityScoreRefreshJob> logger)
{
    private const int BatchSize = 200;

    public async Task ExecuteAsync()
    {
        logger.LogInformation("PriorityScoreRefreshJob: Starting...");

        var now = DateTime.UtcNow;
        var totalUpdated = 0;
        var lastId = Guid.Empty;

        var priorityWeights = ModuleSystemSettings.OfficerPriority(systemSettings);

        while (true)
        {
            var reports = await db.Reports
                .Where(r => r.Status == ReportStatus.Submitted
                         || r.Status == ReportStatus.Verified
                         || r.Status == ReportStatus.Reopened
                         || r.Status == ReportStatus.InProgress)
                .Where(r => r.Id.CompareTo(lastId) > 0)
                .OrderBy(r => r.Id)
                .Take(BatchSize)
                .ToListAsync()
                .ConfigureAwait(false);

            if (reports.Count == 0)
                break;

            foreach (var report in reports)
            {
                var ageHours = (decimal)(now - report.CreatedAt).TotalHours;
                var score = ModuleSystemSettings.ComputePriorityScore(
                    priorityWeights,
                    report.Severity,
                    report.ReporterCount,
                    ageHours,
                    report.SlaVerifyBreached && report.Status == ReportStatus.Submitted);

                report.UpdatePriorityScore(score);
            }

            await db.SaveChangesAsync().ConfigureAwait(false);
            totalUpdated += reports.Count;
            lastId = reports[^1].Id;

            if (reports.Count < BatchSize)
                break;
        }

        logger.LogInformation(
            "PriorityScoreRefreshJob: Updated {Count} reports",
            totalUpdated);
    }
}
