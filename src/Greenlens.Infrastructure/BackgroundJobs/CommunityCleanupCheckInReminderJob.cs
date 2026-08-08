using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// Reminds Community Cleanup participants ~15 minutes before StartsAt to check in.
/// Runs every 5 minutes; catches events starting in the next 10-15 minute window.
/// Idempotent — skips a participant who already received this reminder for the event.
/// </summary>
[AutomaticRetry(Attempts = 2)]
[DisableConcurrentExecution(timeoutInSeconds: 240)]
internal sealed class CommunityCleanupCheckInReminderJob(
    ApplicationDbContext db,
    INotificationService notificationService,
    ILogger<CommunityCleanupCheckInReminderJob> logger)
{
    public async Task ExecuteAsync()
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(10);
        var windowEnd = now.AddMinutes(15);

        var upcomingEvents = await db.CommunityCleanupEvents
            .Where(e => e.Status == CommunityCleanupStatus.OpenForJoin || e.Status == CommunityCleanupStatus.JoinClosed)
            .Where(e => e.StartsAt >= windowStart && e.StartsAt < windowEnd)
            .ToListAsync()
            .ConfigureAwait(false);

        if (upcomingEvents.Count == 0)
        {
            logger.LogInformation("CommunityCleanupCheckInReminderJob: No upcoming events in window.");
            return;
        }

        var eventIds = upcomingEvents.Select(e => e.Id).ToList();

        var alreadyReminded = await db.Notifications
            .AsNoTracking()
            .Where(n => n.Type == NotificationType.CommunityCleanupCheckInReminder
                        && n.ReferenceId != null
                        && eventIds.Contains(n.ReferenceId!.Value))
            .Select(n => new { EventId = n.ReferenceId!.Value, n.RecipientId })
            .ToListAsync()
            .ConfigureAwait(false);

        var alreadyRemindedSet = alreadyReminded
            .Select(x => (x.EventId, x.RecipientId))
            .ToHashSet();

        var sent = 0;

        foreach (var ev in upcomingEvents)
        {
            var participants = await db.CommunityCleanupParticipants
                .AsNoTracking()
                .Where(p => p.EventId == ev.Id
                            && (p.Status == CommunityCleanupParticipantStatus.Joined
                                || p.Status == CommunityCleanupParticipantStatus.CheckedIn))
                .Select(p => new { p.UserId, p.Status })
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var participant in participants)
            {
                if (participant.Status == CommunityCleanupParticipantStatus.CheckedIn)
                    continue;

                if (alreadyRemindedSet.Contains((ev.Id, participant.UserId)))
                    continue;

                await notificationService.SendFromTemplateAsync(
                    participant.UserId,
                    NotificationType.CommunityCleanupCheckInReminder,
                    new Dictionary<string, string> { ["title"] = ev.Title },
                    ev.Id).ConfigureAwait(false);
                sent++;
            }
        }

        logger.LogInformation(
            "CommunityCleanupCheckInReminderJob: Sent {Count} check-in reminder(s) across {EventCount} event(s)",
            sent, upcomingEvents.Count);
    }
}
