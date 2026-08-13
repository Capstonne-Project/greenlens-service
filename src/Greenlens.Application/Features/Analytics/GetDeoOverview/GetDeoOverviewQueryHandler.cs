using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Analytics.GetDeoOverview;

/// <summary>
/// DEO dashboard overview KPIs scoped to the caller's department.
/// </summary>
/// <remarks>Implements: BR-OFF-010 (monitoring), BR-CMP-020 (company context), BR-SYS-001.</remarks>
public sealed class GetDeoOverviewQueryHandler(
    IReportRepository reports,
    IEnvironmentalServiceCompanyRepository companies,
    ILocalOfficeRepository localOffices,
    IUserRepository users,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<GetDeoOverviewQueryHandler> logger)
    : IRequestHandler<GetDeoOverviewQuery, Result<DeoOverviewResponse>>
{
    private static readonly ReportStatus[] PendingStatuses =
        [ReportStatus.Submitted, ReportStatus.Verified, ReportStatus.InProgress, ReportStatus.Reopened];

    private static readonly ReportStatus[] ResolvedStatuses =
        [ReportStatus.Resolved, ReportStatus.Closed];

    public async Task<Result<DeoOverviewResponse>> Handle(
        GetDeoOverviewQuery request, CancellationToken ct)
    {
        var scopeResult = await DepartmentContextResolver
            .ResolveAsync(users, currentUser, ct)
            .ConfigureAwait(false);
        if (scopeResult.IsFailure)
            return scopeResult.Error!;

        var scope = scopeResult.Value!;
        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var deptReports = DepartmentContextResolver.ApplyDepartmentScope(
            reports.QueryAsNoTracking(), scope.DepartmentId);

        var reportsInRange = deptReports.Where(r => r.CreatedAt >= from && r.CreatedAt <= to);

        var totalReports = await reportsInRange.CountAsync(ct).ConfigureAwait(false);
        var pendingReports = await reportsInRange
            .CountAsync(r => PendingStatuses.Contains(r.Status), ct)
            .ConfigureAwait(false);
        var resolvedReports = await reportsInRange
            .CountAsync(r => ResolvedStatuses.Contains(r.Status), ct)
            .ConfigureAwait(false);

        var openReports = deptReports.Where(r => PendingStatuses.Contains(r.Status));
        var slaBreachedCount = await openReports
            .CountAsync(r => r.SlaVerifyBreached || r.SlaResolveBreached, ct)
            .ConfigureAwait(false);
        var duplicateFlagCount = await deptReports
            .CountAsync(r => r.IsPossibleDuplicate, ct)
            .ConfigureAwait(false);
        var recurrenceFlagCount = await deptReports
            .CountAsync(r => r.IsSuspectedViolationRecurrence, ct)
            .ConfigureAwait(false);

        var resolvedInRange = await reportsInRange
            .Where(r => ResolvedStatuses.Contains(r.Status) && r.ResolvedAt != null)
            .Select(r => new { r.VerifiedAt, r.ResolvedAt, r.SlaResolveBreached })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var slaComplianceRate = resolvedInRange.Count == 0
            ? 100m
            : Math.Round(
                100m * resolvedInRange.Count(r => !r.SlaResolveBreached) / resolvedInRange.Count,
                1);

        var resolutionHoursSamples = resolvedInRange
            .Where(r => r.VerifiedAt.HasValue)
            .Select(r => (decimal)(r.ResolvedAt!.Value - r.VerifiedAt!.Value).TotalHours)
            .ToList();

        var averageResolutionHours = resolutionHoursSamples.Count == 0
            ? 0m
            : Math.Round(resolutionHoursSamples.Average(), 1);

        var companyQuery = companies.QueryAsNoTracking()
            .Where(c => c.DepartmentId == scope.DepartmentId);

        var activeCompanies = await companyQuery
            .CountAsync(c => c.Status == CompanyStatus.Active, ct)
            .ConfigureAwait(false);
        var pendingActivationCompanies = await companyQuery
            .CountAsync(c => c.Status == CompanyStatus.PendingActivation, ct)
            .ConfigureAwait(false);

        var localOfficeCount = await localOffices.QueryAsNoTracking()
            .CountAsync(o => o.DepartmentId == scope.DepartmentId, ct)
            .ConfigureAwait(false);
        var onboardedOfficeCount = await localOffices.QueryAsNoTracking()
            .CountAsync(o => o.DepartmentId == scope.DepartmentId && o.IsOnboarded, ct)
            .ConfigureAwait(false);

        logger.LogInformation(
            "DEO overview for department {DepartmentId}: {TotalReports} reports in range",
            scope.DepartmentId, totalReports);

        return new DeoOverviewResponse(
            scope.DepartmentId,
            scope.DepartmentName,
            totalReports,
            pendingReports,
            resolvedReports,
            slaBreachedCount,
            duplicateFlagCount,
            recurrenceFlagCount,
            slaComplianceRate,
            averageResolutionHours,
            activeCompanies,
            pendingActivationCompanies,
            localOfficeCount,
            onboardedOfficeCount);
    }
}
