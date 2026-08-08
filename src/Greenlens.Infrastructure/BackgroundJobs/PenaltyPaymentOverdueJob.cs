using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-INS-021: Mark penalty dossiers overdue when payment deadline passes; notify LEO/DEO.
/// Runs every hour.
/// </summary>
/// <remarks>Implements: BR-INS-021, BR-NTF-002.</remarks>
[AutomaticRetry(Attempts = 2)]
internal sealed class PenaltyPaymentOverdueJob(
    ApplicationDbContext db,
    INotificationService notificationService,
    IOfficerRecipientQuery officerRecipients,
    ILogger<PenaltyPaymentOverdueJob> logger)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("PenaltyPaymentOverdueJob: Starting...");

        var now = DateTime.UtcNow;

        var candidates = await db.InspectionReports
            .Include(ir => ir.Report)
            .Where(ir => ir.Status == InspectionStatus.PenaltyIssued
                      || ir.Status == InspectionStatus.PartiallyPaid)
            .Where(ir => ir.PenaltyDueDate != null && ir.PenaltyDueDate <= now)
            .ToListAsync()
            .ConfigureAwait(false);

        if (candidates.Count == 0)
        {
            logger.LogInformation("PenaltyPaymentOverdueJob: No overdue payments found.");
            return;
        }

        var newlyOverdue = new List<(Guid ReportId, string ReportCode, Guid? OfficeId, Guid? DepartmentId, Guid CreatedByOfficerId, string? DecisionNumber)>();

        foreach (var inspection in candidates)
        {
            if (inspection.MarkOverdue().IsFailure)
                continue;

            var report = inspection.Report;
            newlyOverdue.Add((
                inspection.ReportId,
                report?.Code ?? "liên quan",
                report?.AssignedOfficeId,
                report?.AssignedDepartmentId,
                inspection.CreatedByOfficerId,
                inspection.PenaltyDecisionNumber));
        }

        if (newlyOverdue.Count == 0)
        {
            logger.LogInformation("PenaltyPaymentOverdueJob: Candidates found but none transitioned to Overdue.");
            return;
        }

        await db.SaveChangesAsync().ConfigureAwait(false);

        foreach (var item in newlyOverdue)
        {
            var placeholders = JobNotificationPlaceholders.ForPenaltyPaymentOverdue(
                item.ReportCode,
                item.DecisionNumber ?? "chưa có");
            placeholders = await JobNotificationPlaceholders
                .EnrichFromReportIdAsync(db, placeholders, item.ReportId)
                .ConfigureAwait(false);

            var recipientIds = await InspectionOfficerRecipientResolver
                .ResolveAsync(
                    officerRecipients,
                    item.OfficeId,
                    item.DepartmentId,
                    item.CreatedByOfficerId,
                    CancellationToken.None)
                .ConfigureAwait(false);

            if (recipientIds.Count == 0)
            {
                logger.LogWarning(
                    "PenaltyPaymentOverdueJob: no officers to notify for report {ReportCode}",
                    item.ReportCode);
                continue;
            }

            foreach (var recipientId in recipientIds)
            {
                await notificationService.SendFromTemplateAsync(
                    recipientId,
                    NotificationType.PenaltyPaymentOverdue,
                    placeholders,
                    item.ReportId).ConfigureAwait(false);
            }
        }

        logger.LogWarning(
            "PenaltyPaymentOverdueJob: Marked {Count} inspection(s) overdue and sent notifications",
            newlyOverdue.Count);
    }
}
