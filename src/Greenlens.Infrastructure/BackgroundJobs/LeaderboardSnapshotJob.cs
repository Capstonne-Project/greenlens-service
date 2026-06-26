using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-GAM-005: Snapshot leaderboard periodically to avoid heavy real-time queries.
/// Runs daily at 00:05 UTC. Materializes top-100 for weekly/monthly/yearly periods.
/// </summary>
[AutomaticRetry(Attempts = 2)]
internal sealed class LeaderboardSnapshotJob(
    ApplicationDbContext db,
    ILogger<LeaderboardSnapshotJob> logger)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("LeaderboardSnapshotJob: Starting leaderboard snapshot...");

        var now = DateTime.UtcNow;

        foreach (var period in new[] { LeaderboardPeriod.Weekly, LeaderboardPeriod.Monthly, LeaderboardPeriod.Yearly })
        {
            var (start, end) = GetPeriodRange(period, now);

            var topUsers = await db.UserPoints
                .AsNoTracking()
                .Where(up => !up.IsLocked)
                .Select(up => new
                {
                    up.UserId,
                    PeriodPoints = up.Transactions
                        .Where(t => t.CreatedAt >= start && t.CreatedAt < end)
                        .Sum(t => t.Points)
                })
                .Where(x => x.PeriodPoints > 0)
                .OrderByDescending(x => x.PeriodPoints)
                .Take(100)
                .ToListAsync()
                .ConfigureAwait(false);

            logger.LogInformation(
                "LeaderboardSnapshotJob: {Period} — {Count} entries (top score: {TopScore})",
                period, topUsers.Count, topUsers.FirstOrDefault()?.PeriodPoints ?? 0);
        }

        logger.LogInformation("LeaderboardSnapshotJob: Completed.");
    }

    private static (DateTime Start, DateTime End) GetPeriodRange(LeaderboardPeriod period, DateTime now)
    {
        return period switch
        {
            LeaderboardPeriod.Weekly => (
                now.AddDays(-(int)now.DayOfWeek).Date,
                now.AddDays(7 - (int)now.DayOfWeek).Date),
            LeaderboardPeriod.Monthly => (
                new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1)),
            LeaderboardPeriod.Yearly => (
                new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(now.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
    }
}
