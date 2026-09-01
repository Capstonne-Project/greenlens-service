using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-REP-019: Hard-delete drafts not updated in 7 days.
/// Runs daily at 03:00 UTC. Processes in batches.
/// </summary>
[AutomaticRetry(Attempts = 2)]
internal sealed class DraftCleanupJob(
    ApplicationDbContext db,
    ILogger<DraftCleanupJob> logger)
{
    private const int RetentionDays = 7;
    private const int BatchSize = 200;

    public async Task ExecuteAsync()
    {
        logger.LogInformation("DraftCleanupJob: Starting...");

        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
        var totalDeleted = 0;

        while (true)
        {
            var staleDrafts = await db.ReportDrafts
                .Where(d => d.UpdatedAt <= cutoff)
                .OrderBy(d => d.Id)
                .Take(BatchSize)
                .ToListAsync()
                .ConfigureAwait(false);

            if (staleDrafts.Count == 0)
                break;

            db.ReportDrafts.RemoveRange(staleDrafts);
            await db.SaveChangesAsync().ConfigureAwait(false);
            totalDeleted += staleDrafts.Count;

            if (staleDrafts.Count < BatchSize)
                break;
        }

        logger.LogInformation(
            "DraftCleanupJob: Completed. Deleted {Count} stale drafts (> {Days} days idle)",
            totalDeleted, RetentionDays);
    }
}
