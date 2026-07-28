using Greenlens.Application.Common.Interfaces;
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
/// <remarks>Implements: BR-INS-030, BR-NTF-002.</remarks>
[AutomaticRetry(Attempts = 2)]
internal sealed class SlaBreachInspectionJob(
    ApplicationDbContext db,
    INotificationService notificationService,
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
            inspection.MarkSlaInspectionBreached();

        await db.SaveChangesAsync().ConfigureAwait(false);

        foreach (var inspection in breachedInspections)
        {
            var reportCode = inspection.Report?.Code ?? "liên quan";

            var placeholders = JobNotificationPlaceholders.ForReport(reportCode);
            placeholders = await JobNotificationPlaceholders
                .EnrichFromReportIdAsync(db, placeholders, inspection.ReportId)
                .ConfigureAwait(false);

            await notificationService.SendFromTemplateAsync(
                inspection.CreatedByOfficerId,
                NotificationType.SlaInspectionBreached,
                placeholders,
                inspection.ReportId).ConfigureAwait(false);
        }

        logger.LogWarning(
            "SlaBreachInspectionJob: Flagged {Count} inspections with SLA breach",
            breachedInspections.Count);
    }
}
