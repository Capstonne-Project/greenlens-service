using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Services;

/// <summary>
/// BR-CMP-013: Cascading deactivation — when a company is suspended/terminated/expired,
/// auto-decline all active assignments from the company's teams, revert orphaned reports
/// to Verified, and notify LEO for reassignment.
/// </summary>
/// <remarks>Implements: BR-CMP-013, BR-NTF-002.</remarks>
internal sealed class CompanyCascadeService(
    IReportAssignmentRepository assignments,
    IReportRepository reports,
    ApplicationDbContext db,
    INotificationService notificationService,
    ILogger<CompanyCascadeService> logger) : ICompanyCascadeService
{
    public async Task CascadeDeactivationAsync(Guid companyId, string reason, CancellationToken ct)
    {
        // Lấy danh sách các assignment được gán cho công ty
        var activeAssignments = await assignments
            .GetActiveByCompanyTeamsAsync(companyId, ct)
            .ConfigureAwait(false);

        if (activeAssignments.Count == 0)
        {
            logger.LogInformation("CompanyCascade: No active assignments for company {CompanyId}", companyId);
            return;
        }
        // Lấy danh sách các report ID được gán cho công ty
        var affectedReportIds = activeAssignments.Select(a => a.ReportId).Distinct().ToList();

        foreach (var assignment in activeAssignments)
        {
            if (assignment.Status == AssignmentStatus.Assigned)
                assignment.Decline(reason);
            else if (assignment.Status == AssignmentStatus.InProgress)
                assignment.ForceDecline(reason);
        }

        // Lấy danh sách các officer ID được gán cho công ty
        var notifyTargets = new List<(Guid RecipientId, Guid ReportId, string ReportCode)>();

        foreach (var reportId in affectedReportIds)
        {
            var report = await reports.GetByIdAsync(reportId, ct).ConfigureAwait(false);
            if (report is null || report.Status != ReportStatus.InProgress)
                continue;

            report.RevertToVerified();

            var leoId = await ResolveOfficerIdAsync(report, ct).ConfigureAwait(false);
            if (leoId.HasValue)
                notifyTargets.Add((leoId.Value, report.Id, report.Code));
        }
    
        foreach (var (recipientId, reportId, reportCode) in notifyTargets)
        {
            await notificationService.SendFromTemplateAsync(
                recipientId,
                NotificationType.CompanyDeactivationReassign,
                NotificationPlaceholders.ForCompanyDeactivationReassign(reportCode),
                reportId,
                ct).ConfigureAwait(false);
        }

        logger.LogWarning(
            "CompanyCascade: Declined {AssignmentCount} assignments, reverted {ReportCount} reports, notified {NotifyCount} officer(s) for company {CompanyId}",
            activeAssignments.Count, affectedReportIds.Count, notifyTargets.Count, companyId);
    }

    private async Task<Guid?> ResolveOfficerIdAsync(Report report, CancellationToken ct)
    {
        if (report.AssignedOfficeId.HasValue)
        {
            var leoId = await db.Users
                .AsNoTracking()
                .Where(u => u.LocalOfficeId == report.AssignedOfficeId && !u.IsBanned)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (leoId != Guid.Empty)
                return leoId;
        }
        
        if (report.AssignedDepartmentId.HasValue)
        {
            var deoId = await db.Users
                .AsNoTracking()
                .Where(u => u.DepartmentId == report.AssignedDepartmentId && !u.IsBanned)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (deoId != Guid.Empty)
                return deoId;
        }

        return null;
    }
}
