using Greenlens.Domain.Entities;
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

            // BR-ORG-014: Escalate to Department queue — DEO takes over
            report.EscalateToDepartment();

            // BR-OFF-002: Notify LEO (if assigned)
            if (report.AssignedOfficeId.HasValue)
            {
                var leoId = await db.Users
                    .Where(u => u.LocalOfficeId == report.AssignedOfficeId && !u.IsBanned)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);

                if (leoId != Guid.Empty)
                {
                    db.Notifications.Add(Notification.Create(
                        leoId,
                        NotificationType.SlaBreachWarning,
                        "Vượt SLA xác minh",
                        $"Báo cáo {report.Code} đã vượt SLA xác minh 24h và được chuyển lên cấp trên.",
                        referenceId: report.Id));
                }
            }

            // BR-OFF-002: Notify DEO (escalation target)
            if (report.AssignedDepartmentId.HasValue)
            {
                var deoId = await db.Users
                    .Where(u => u.DepartmentId == report.AssignedDepartmentId && !u.IsBanned)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);

                if (deoId != Guid.Empty)
                {
                    db.Notifications.Add(Notification.Create(
                        deoId,
                        NotificationType.SlaBreachWarning,
                        "Tiếp nhận báo cáo escalated",
                        $"Báo cáo {report.Code} vượt SLA xác minh — đã chuyển vào hàng đợi của bạn.",
                        referenceId: report.Id));
                }
            }
        }

        await db.SaveChangesAsync().ConfigureAwait(false);

        logger.LogWarning(
            "SlaBreachVerificationJob: Flagged {Count} reports with SLA verification breach",
            breachedReports.Count);
    }
}
