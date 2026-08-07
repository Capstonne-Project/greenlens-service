using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-INS-030: Flag InspectionReports that exceeded SLA; auto-close when no inspector conclusion.
/// Runs every 30 minutes.
/// </summary>
/// <remarks>Implements: BR-INS-030, BR-INS-013, BR-NTF-002.</remarks>
[AutomaticRetry(Attempts = 2)]
internal sealed class SlaBreachInspectionJob(
    ApplicationDbContext db,
    INotificationService notificationService,
    IInspectionClosedNoViolationNotifier closedNoViolationNotifier,
    ILogger<SlaBreachInspectionJob> logger)
{
    private const string AutoCloseReason =
        "Hết hạn SLA điều tra theo BR-INS-030. Hệ thống tự động đóng hồ sơ vì Inspection Team chưa ban hành kết luận xử phạt hoặc biên bản không vi phạm trong thời hạn quy định.";

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

        var autoClosedReports = new List<(Guid ReportId, string ReportCode, Guid? ReporterId)>();

        foreach (var inspection in breachedInspections)
        {
            inspection.MarkSlaInspectionBreached();

            if (inspection.Status is InspectionStatus.Draft or InspectionStatus.InProgress)
            {
                var closeResult = inspection.ForceCloseNoViolation(AutoCloseReason);
                if (closeResult.IsFailure)
                {
                    logger.LogWarning(
                        "SlaBreachInspectionJob: Could not auto-close inspection {Id}: {Error}",
                        inspection.Id, closeResult.Error?.Code);
                }
                else if (inspection.Report is not null)
                {
                    autoClosedReports.Add((
                        inspection.ReportId,
                        inspection.Report.Code,
                        inspection.Report.ReporterId));
                }
            }
        }

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
                inspection.Id).ConfigureAwait(false);
        }

        foreach (var (reportId, reportCode, reporterId) in autoClosedReports)
        {
            await closedNoViolationNotifier
                .NotifyReporterAsync(reportId, reportCode, reporterId, AutoCloseReason, CancellationToken.None)
                .ConfigureAwait(false);
        }

        logger.LogWarning(
            "SlaBreachInspectionJob: Processed {Count} SLA breaches (flagged + auto-closed when applicable)",
            breachedInspections.Count);
    }
}
