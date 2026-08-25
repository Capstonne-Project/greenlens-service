using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Analytics.GetAdminAlerts;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Analytics.GetDeoAlerts;

/// <summary>Operational alerts for DEO dashboard, scoped to department.</summary>
/// <remarks>Implements: BR-OFF-010, BR-CMP-007.</remarks>
public sealed class GetDeoAlertsQueryHandler(
    IReportRepository reports,
    IEnvironmentalServiceCompanyRepository companies,
    IUserRepository users,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ISystemSettingsProvider systemSettings,
    ILogger<GetDeoAlertsQueryHandler> logger)
    : IRequestHandler<GetDeoAlertsQuery, Result<List<AlertItem>>>
{
    private static readonly ReportStatus[] OpenStatuses =
        [ReportStatus.Submitted, ReportStatus.Verified, ReportStatus.InProgress, ReportStatus.Reopened];

    public async Task<Result<List<AlertItem>>> Handle(GetDeoAlertsQuery request, CancellationToken ct)
    {
        var scopeResult = await DepartmentContextResolver.ResolveAsync(users, currentUser, ct).ConfigureAwait(false);
        if (scopeResult.IsFailure)
            return scopeResult.Error!;

        var scope = scopeResult.Value!;
        var deptId = scope.DepartmentId;
        var overduePendingHours = ModuleSystemSettings.SlaOverduePendingHours(systemSettings);
        var contractAlertHorizonDays = ModuleSystemSettings.ContractAlertHorizonDays(systemSettings);
        var deptReports = DepartmentContextResolver.ApplyDepartmentScope(
            reports.QueryAsNoTracking(), deptId);

        var alerts = new List<AlertItem>();

        var slaBreachCount = await deptReports
            .CountAsync(r => OpenStatuses.Contains(r.Status)
                              && (r.SlaVerifyBreached || r.SlaResolveBreached), ct)
            .ConfigureAwait(false);
        if (slaBreachCount > 0)
            alerts.Add(new AlertItem("SLA_BREACH", "High", $"{slaBreachCount} báo cáo vượt quá thời hạn SLA trong tỉnh."));

        var overdueCount = await deptReports
            .CountAsync(r => OpenStatuses.Contains(r.Status) && r.IsOverdue, ct)
            .ConfigureAwait(false);
        if (overdueCount > 0)
            alerts.Add(new AlertItem("OVERDUE_REPORTS", "Medium", $"{overdueCount} báo cáo đang chờ xử lý quá {overduePendingHours} giờ."));

        var duplicateCount = await deptReports.CountAsync(r => r.IsPossibleDuplicate, ct).ConfigureAwait(false);
        if (duplicateCount > 0)
            alerts.Add(new AlertItem("POSSIBLE_DUPLICATES", "Low", $"{duplicateCount} báo cáo nghi trùng lặp đang chờ LEO xử lý."));

        var recurrenceCount = await deptReports
            .CountAsync(r => r.IsSuspectedViolationRecurrence, ct)
            .ConfigureAwait(false);
        if (recurrenceCount > 0)
            alerts.Add(new AlertItem("VIOLATION_RECURRENCE", "Medium", $"{recurrenceCount} báo cáo nghi tái phạm vi phạm."));

        var reopenCount = await deptReports.CountAsync(r => r.HasPendingReopenRequest, ct).ConfigureAwait(false);
        if (reopenCount > 0)
            alerts.Add(new AlertItem("PENDING_REOPEN", "Medium", $"{reopenCount} yêu cầu mở lại báo cáo đang chờ LEO duyệt."));

        var now = clock.UtcNow;
        var contractWarningDate = now.AddDays(contractAlertHorizonDays);
        var expiringContracts = await companies.QueryAsNoTracking()
            .CountAsync(c => c.DepartmentId == deptId
                              && c.Status == CompanyStatus.Active
                              && c.ContractEndDate != null
                              && c.ContractEndDate <= contractWarningDate, ct)
            .ConfigureAwait(false);
        if (expiringContracts > 0)
            alerts.Add(new AlertItem("CONTRACT_EXPIRY", "Medium", $"{expiringContracts} hợp đồng công ty sắp hết hạn trong {contractAlertHorizonDays} ngày."));

        var pendingActivation = await companies.QueryAsNoTracking()
            .CountAsync(c => c.DepartmentId == deptId && c.Status == CompanyStatus.PendingActivation, ct)
            .ConfigureAwait(false);
        if (pendingActivation > 0)
            alerts.Add(new AlertItem("COMPANY_PENDING_ACTIVATION", "Low", $"{pendingActivation} công ty chờ CM kích hoạt tài khoản."));

        logger.LogInformation("DEO alerts for department {DepartmentId}: {Count} items", deptId, alerts.Count);
        return alerts;
    }
}
