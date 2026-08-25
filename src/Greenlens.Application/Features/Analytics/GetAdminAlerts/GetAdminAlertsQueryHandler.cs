using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Analytics.GetAdminAlerts;

/// <summary>System-wide operational alerts: SLA breaches, overdue reports, unresolved possible duplicates.</summary>
public sealed class GetAdminAlertsQueryHandler(
    IReportRepository reports,
    ISystemSettingsProvider systemSettings,
    ILogger<GetAdminAlertsQueryHandler> logger)
    : IRequestHandler<GetAdminAlertsQuery, Result<List<AlertItem>>>
{
    private static readonly ReportStatus[] OpenStatuses =
        [ReportStatus.Submitted, ReportStatus.Verified, ReportStatus.InProgress];

    public async Task<Result<List<AlertItem>>> Handle(
        GetAdminAlertsQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting admin alerts");

        var overduePendingHours = ModuleSystemSettings.SlaOverduePendingHours(systemSettings);
        var alerts = new List<AlertItem>();

        var slaBreachCount = await reports.QueryAsNoTracking()
            .CountAsync(r => OpenStatuses.Contains(r.Status)
                              && (r.SlaVerifyBreached || r.SlaResolveBreached), ct)
            .ConfigureAwait(false);
        logger.LogInformation("SLA breach count: {SlaBreachCount}", slaBreachCount);

        if (slaBreachCount > 0)
            alerts.Add(new AlertItem(
                "SLA_BREACH",
                "High",
                $"{slaBreachCount} báo cáo vượt quá thời hạn SLA."));

        var overdueCount = await reports.QueryAsNoTracking()
            .CountAsync(r => OpenStatuses.Contains(r.Status) && r.IsOverdue, ct)
            .ConfigureAwait(false);
        logger.LogInformation("Overdue count: {OverdueCount}", overdueCount);

        if (overdueCount > 0)
            alerts.Add(new AlertItem(
                "OVERDUE_REPORTS",
                "Medium",
                $"{overdueCount} báo cáo đang chờ xử lý quá {overduePendingHours} giờ."));

        var possibleDuplicateCount = await reports.QueryAsNoTracking()
            .CountAsync(r => r.IsPossibleDuplicate, ct)
            .ConfigureAwait(false);
        logger.LogInformation("Possible duplicate count: {PossibleDuplicateCount}", possibleDuplicateCount);

        if (possibleDuplicateCount > 0)
            alerts.Add(new AlertItem(
                "POSSIBLE_DUPLICATES",
                "Low",
                $"{possibleDuplicateCount} báo cáo nghi trùng lặp đang chờ xác nhận."));

        var suspiciousCount = await reports.QueryAsNoTracking()
            .CountAsync(r => r.IsSuspicious && OpenStatuses.Contains(r.Status), ct)
            .ConfigureAwait(false);
        logger.LogInformation("Suspicious count: {SuspiciousCount}", suspiciousCount);

        if (suspiciousCount > 0)
            alerts.Add(new AlertItem(
                "SUSPICIOUS_REPORTS",
                "Medium",
                $"{suspiciousCount} báo cáo bị gắn cờ khả nghi bởi AI."));

        logger.LogInformation("Admin alerts retrieved successfully");

        return alerts;
    }
}
