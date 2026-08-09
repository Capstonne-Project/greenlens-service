using Greenlens.Application.Features.Gamification.CheckBadges;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-GAM-004: Periodic safety-net recheck of badge eligibility for every gamification user.
/// </summary>
/// <remarks>
/// CheckBadgesCommand normally runs as a side-effect of point-awarding events (ReportVerified,
/// ReportResolved, PenaltyIssued, CommunityCleanupParticipation) and report submission (for
/// streak badges). But some progress axes — most notably submit-streak days — advance purely
/// with the passage of calendar time and are only sampled at the moment those events fire.
/// A user whose most recent qualifying submission is still pending review (or was rejected,
/// which intentionally does not trigger a recheck) can end up with a streak/report count that
/// already qualifies for a badge without ever having a check run against the up-to-date value.
/// This job is the backfill/safety net: it walks every UserPoints row and reruns eligibility.
/// CheckBadgesCommand is idempotent (skips already-owned badges), so re-running it is always safe.
/// Runs nightly — badge unlocks are not time-critical enough to warrant a tighter cadence.
/// </remarks>
[AutomaticRetry(Attempts = 2)]
internal sealed class BadgeRecheckJob(
    ApplicationDbContext db,
    ISender sender,
    ILogger<BadgeRecheckJob> logger)
{
    private const int BatchSize = 200;

    public async Task ExecuteAsync()
    {
        logger.LogInformation("BadgeRecheckJob: Starting...");

        var totalChecked = 0;
        var totalAwarded = 0;
        var lastId = Guid.Empty;

        while (true)
        {
            var userIds = await db.UserPoints
                .AsNoTracking()
                .Where(up => !up.IsLocked)
                .Where(up => up.Id.CompareTo(lastId) > 0)
                .OrderBy(up => up.Id)
                .Select(up => new { up.Id, up.UserId })
                .Take(BatchSize)
                .ToListAsync()
                .ConfigureAwait(false);

            if (userIds.Count == 0)
                break;

            foreach (var entry in userIds)
            {
                try
                {
                    var result = await sender
                        .Send(new CheckBadgesCommand(entry.UserId))
                        .ConfigureAwait(false);

                    if (result.IsSuccess && result.Value!.NewlyAwarded.Count > 0)
                        totalAwarded += result.Value.NewlyAwarded.Count;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex, "BadgeRecheckJob: failed to recheck badges for user {UserId}", entry.UserId);
                }
            }

            totalChecked += userIds.Count;
            lastId = userIds[^1].Id;

            if (userIds.Count < BatchSize)
                break;
        }

        logger.LogInformation(
            "BadgeRecheckJob: Completed. Checked {Checked} users, awarded {Awarded} new badges.",
            totalChecked, totalAwarded);
    }
}
