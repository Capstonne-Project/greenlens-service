using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-CLN-004: Flag cleanup assignments with stale progress updates.
/// > 24h since last update → notify team (reminder).
/// > 48h → escalate flag, notify LEO.
/// Runs every 1 hour.
/// </summary>
/// <remarks>Implements: BR-CLN-004, BR-NTF-002.</remarks>
[AutomaticRetry(Attempts = 2)]
internal sealed class CleanupProgressSlaJob(
    ApplicationDbContext db,
    INotificationService notificationService,
    ILogger<CleanupProgressSlaJob> logger)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("CleanupProgressSlaJob: Starting...");

        var now = DateTime.UtcNow;
        var threshold24h = now.AddHours(-24);
        var threshold48h = now.AddHours(-48);

        var staleAssignments = await db.ReportAssignments
            .Include(a => a.Report)
            .Where(a => a.Status == AssignmentStatus.InProgress)
            .Where(a => a.ProgressUpdatedAt == null
                ? a.StartedAt < threshold24h
                : a.ProgressUpdatedAt < threshold24h)
            .ToListAsync()
            .ConfigureAwait(false);

        if (staleAssignments.Count == 0)
        {
            logger.LogInformation("CleanupProgressSlaJob: No stale assignments.");
            return;
        }

        var teamNames = await db.EnvironmentalTeams
            .IgnoreQueryFilters()
            .Where(t => staleAssignments.Select(a => a.TeamId).Distinct().Contains(t.Id))
            .Select(t => new { t.Id, t.Name })
            .ToDictionaryAsync(t => t.Id, t => t.Name)
            .ConfigureAwait(false);

        var notified = 0;
        var escalated = 0;

        foreach (var assignment in staleAssignments)
        {
            var lastUpdate = assignment.ProgressUpdatedAt ?? assignment.StartedAt;
            var isEscalation = lastUpdate < threshold48h;

            if (assignment.Report is null)
                continue;

            Guid? leoId = null;
            if (assignment.Report.AssignedOfficeId.HasValue)
            {
                leoId = await db.Users
                    .AsNoTracking()
                    .Where(u => u.LocalOfficeId == assignment.Report.AssignedOfficeId && !u.IsBanned)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
            }

            if (isEscalation && leoId.HasValue && leoId != Guid.Empty)
            {
                var teamName = teamNames.GetValueOrDefault(assignment.TeamId) ?? "đội xử lý";

                await notificationService.SendFromTemplateAsync(
                    leoId.Value,
                    NotificationType.CleanupProgressStale,
                    JobNotificationPlaceholders.ForCleanupStale(
                        assignment.Report.Code,
                        teamName),
                    assignment.ReportId).ConfigureAwait(false);
                escalated++;
            }
            else
            {
                notified++;
            }
        }

        logger.LogWarning(
            "CleanupProgressSlaJob: {Notified} reminders, {Escalated} escalations",
            notified, escalated);
    }
}
