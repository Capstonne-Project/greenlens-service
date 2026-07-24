using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Analytics.GetAdminOverview;

/// <summary>
/// Admin dashboard overview KPIs: user/report counts, SLA compliance, average resolution time.
/// </summary>
public sealed class GetAdminOverviewQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    IEnvironmentalServiceCompanyRepository companies,
    IEnvironmentalTeamRepository teams,
    IDateTimeProvider clock,
    ILogger<GetAdminOverviewQueryHandler> logger)
    : IRequestHandler<GetAdminOverviewQuery, Result<AdminOverviewResponse>>
{
    private static readonly ReportStatus[] PendingStatuses =
        [ReportStatus.Submitted, ReportStatus.Verified, ReportStatus.InProgress];
    private static readonly ReportStatus[] ResolvedStatuses =
        [ReportStatus.Resolved, ReportStatus.Closed];

    public async Task<Result<AdminOverviewResponse>> Handle(
        GetAdminOverviewQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting admin overview");

        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var reportsInRange = reports.QueryAsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to);

        logger.LogInformation("Reports in range: {ReportsInRange}", reportsInRange);

        var totalUsers = await users.QueryAsNoTracking().CountAsync(ct).ConfigureAwait(false);
        var totalReports = await reportsInRange.CountAsync(ct).ConfigureAwait(false);
        var pendingReports = await reportsInRange
            .CountAsync(r => PendingStatuses.Contains(r.Status), ct)
            .ConfigureAwait(false);
        var resolvedReports = await reportsInRange
            .CountAsync(r => ResolvedStatuses.Contains(r.Status), ct)
            .ConfigureAwait(false);

        var activeCompanies = await companies.QueryAsNoTracking()
            .CountAsync(c => c.Status == CompanyStatus.Active, ct)
            .ConfigureAwait(false);
        var activeTeams = await teams.QueryAsNoTracking()
            .CountAsync(t => t.IsActive, ct)
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

        logger.LogInformation("Admin overview retrieved successfully");

        return new AdminOverviewResponse(
            totalUsers,
            totalReports,
            pendingReports,
            resolvedReports,
            activeCompanies,
            activeTeams,
            slaComplianceRate,
            averageResolutionHours);
    }
}
