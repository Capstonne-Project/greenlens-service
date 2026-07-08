using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Services;

/// <summary>
/// BR-CMP-013: Cascading deactivation — when a company is suspended/terminated/expired,
/// auto-decline all active assignments from the company's teams, revert orphaned reports
/// to Verified, and notify LEO for reassignment.
/// </summary>
internal sealed class CompanyCascadeService(
    IReportAssignmentRepository assignments,
    IReportRepository reports,
    INotificationRepository notifications,
    ILogger<CompanyCascadeService> logger) : ICompanyCascadeService
{
    public async Task CascadeDeactivationAsync(Guid companyId, string reason, CancellationToken ct)
    {
        var activeAssignments = await assignments
            .GetActiveByCompanyTeamsAsync(companyId, ct)
            .ConfigureAwait(false);

        if (activeAssignments.Count == 0)
        {
            logger.LogInformation("CompanyCascade: No active assignments for company {CompanyId}", companyId);
            return;
        }

        var affectedReportIds = activeAssignments.Select(a => a.ReportId).Distinct().ToList();

        // Decline/cancel all assignments:
        // - Assigned → use domain Decline() method
        // - InProgress → use ForceDecline() since domain method only allows from Assigned
        foreach (var assignment in activeAssignments)
        {
            if (assignment.Status == AssignmentStatus.Assigned)
            {
                assignment.Decline(reason);
            }
            else if (assignment.Status == AssignmentStatus.InProgress)
            {
                // BR-CMP-013: system-level cancellation of in-progress work
                assignment.ForceDecline(reason);
            }
        }

        // For each affected report: revert InProgress reports to Verified
        foreach (var reportId in affectedReportIds)
        {
            var report = await reports.GetByIdAsync(reportId, ct).ConfigureAwait(false);
            if (report is null || report.Status != ReportStatus.InProgress)
                continue;

            report.RevertToVerified();

            // Notify LEO responsible for this report's area
            if (report.AssignedOfficeId.HasValue)
            {
                notifications.Add(Notification.Create(
                    report.AssignedOfficeId.Value,
                    NotificationType.ReportStatusChanged,
                    "Công ty bị ngưng — cần tái điều phối",
                    $"Báo cáo {report.Code} đã quay về Verified do công ty bị ngưng/chấm dứt. Vui lòng gán đội khác.",
                    referenceId: report.Id));
            }
        }

        logger.LogWarning(
            "CompanyCascade: Declined {AssignmentCount} assignments, reverted {ReportCount} reports for company {CompanyId}",
            activeAssignments.Count, affectedReportIds.Count, companyId);
    }
}
