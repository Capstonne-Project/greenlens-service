using Greenlens.Domain.Entities;
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
[AutomaticRetry(Attempts = 2)]
internal sealed class CleanupProgressSlaJob(
    ApplicationDbContext db,
    ILogger<CleanupProgressSlaJob> logger)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("CleanupProgressSlaJob: Starting...");

        var now = DateTime.UtcNow;
        var threshold24h = now.AddHours(-24);
        var threshold48h = now.AddHours(-48);

        // Assignments InProgress that haven't been updated in > 24h
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

        var notified = 0;
        var escalated = 0;

        foreach (var assignment in staleAssignments)
        {
            var lastUpdate = assignment.ProgressUpdatedAt ?? assignment.StartedAt;
            var isEscalation = lastUpdate < threshold48h;

            if (assignment.Report is null) continue;

            // Find LEO for this report's office
            Guid? leoId = null;
            if (assignment.Report.AssignedOfficeId.HasValue)
            {
                leoId = await db.Users
                    .Where(u => u.LocalOfficeId == assignment.Report.AssignedOfficeId && !u.IsBanned)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
            }

            if (isEscalation && leoId.HasValue && leoId != Guid.Empty)
            {
                // > 48h → notify LEO
                db.Notifications.Add(Notification.Create(
                    leoId.Value,
                    NotificationType.SlaBreachWarning,
                    "Cleanup tiến độ trễ >48h",
                    $"Đội {assignment.TeamId} chưa cập nhật tiến độ >48h cho báo cáo {assignment.Report.Code}.",
                    referenceId: assignment.ReportId));
                escalated++;
            }
            else
            {
                // > 24h → notify team (via assignment — team members can see)
                notified++;
            }
        }

        await db.SaveChangesAsync().ConfigureAwait(false);

        logger.LogWarning(
            "CleanupProgressSlaJob: {Notified} reminders, {Escalated} escalations",
            notified, escalated);
    }
}
