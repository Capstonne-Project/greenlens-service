using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-INS-030: Flag InspectionReports that have exceeded their severity-based
/// SLA deadline. Runs every 30 minutes.
/// SLA durations: Critical=3d, High=5d, Medium=7d, Low=10d (from creation).
/// </summary>
[AutomaticRetry(Attempts = 2)]
internal sealed class SlaBreachInspectionJob(
    ApplicationDbContext db,
    ILogger<SlaBreachInspectionJob> logger)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("SlaBreachInspectionJob: Starting...");

        var now = DateTime.UtcNow;

        var breachedInspections = await db.InspectionReports
            .Include(ir => ir.Report)
            .Where(ir => ir.Status == InspectionStatus.Draft
                      || ir.Status == InspectionStatus.InProgress)
            .Where(ir => ir.SlaInspectionDueAt != null
                      && ir.SlaInspectionDueAt <= now
                      && !ir.SlaInspectionBreached)
            .ToListAsync()
            .ConfigureAwait(false);

        if (breachedInspections.Count == 0)
        {
            logger.LogInformation("SlaBreachInspectionJob: No breaches found.");
            return;
        }

        foreach (var inspection in breachedInspections)
        {
            inspection.MarkSlaInspectionBreached();

            var reportCode = inspection.Report?.Code ?? "liên quan";

            // Notify LEO who created this inspection
            db.Notifications.Add(Notification.Create(
                inspection.CreatedByOfficerId,
                NotificationType.SlaBreachWarning,
                "Vượt SLA xử phạt",
                $"Hồ sơ xử phạt liên quan báo cáo {reportCode} đã vượt SLA. Vui lòng kiểm tra.",
                referenceId: inspection.ReportId));
        }

        await db.SaveChangesAsync().ConfigureAwait(false);

        logger.LogWarning(
            "SlaBreachInspectionJob: Flagged {Count} inspections with SLA breach",
            breachedInspections.Count);
    }
}
