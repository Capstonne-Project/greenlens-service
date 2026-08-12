using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Analytics.GetAdminCompanyPerformance;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Analytics.GetDeoCompanyPerformance;

public sealed class GetDeoCompanyPerformanceQueryHandler(
    IReportRepository reports,
    IEnvironmentalServiceCompanyRepository companies,
    IUserRepository users,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<GetDeoCompanyPerformanceQueryHandler> logger)
    : IRequestHandler<GetDeoCompanyPerformanceQuery, Result<List<CompanyPerformanceItem>>>
{
    public async Task<Result<List<CompanyPerformanceItem>>> Handle(
        GetDeoCompanyPerformanceQuery request, CancellationToken ct)
    {
        var scopeResult = await DepartmentContextResolver.ResolveAsync(users, currentUser, ct).ConfigureAwait(false);
        if (scopeResult.IsFailure)
            return scopeResult.Error!;

        var deptId = scopeResult.Value.DepartmentId;
        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var companyIds = await companies.QueryAsNoTracking()
            .Where(c => c.DepartmentId == deptId)
            .Select(c => c.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (companyIds.Count == 0)
            return new List<CompanyPerformanceItem>();

        var dispatched = await DepartmentContextResolver
            .ApplyDepartmentScope(reports.QueryAsNoTracking(), deptId)
            .Where(r => r.AssignedCompanyId != null
                        && companyIds.Contains(r.AssignedCompanyId.Value)
                        && r.DispatchedToCompanyAt >= from && r.DispatchedToCompanyAt <= to)
            .Select(r => new
            {
                CompanyId = r.AssignedCompanyId!.Value,
                r.Status,
                r.ResolvedAt,
                r.SlaResolveDueAt,
                r.SlaResolveBreached
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var companyNames = await companies.QueryAsNoTracking()
            .Where(c => companyIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct)
            .ConfigureAwait(false);

        var result = dispatched
            .GroupBy(r => r.CompanyId)
            .Select(g =>
            {
                var assigned = g.Count();
                var completed = g.Count(r => r.Status is ReportStatus.Resolved or ReportStatus.Closed);
                var onTimeCompleted = g.Count(r =>
                    r.Status is ReportStatus.Resolved or ReportStatus.Closed
                    && r.ResolvedAt.HasValue
                    && (!r.SlaResolveDueAt.HasValue || r.ResolvedAt.Value <= r.SlaResolveDueAt.Value));
                var notBreached = g.Count(r => !r.SlaResolveBreached);

                var onTimeRate = completed == 0 ? 0m : Math.Round(100m * onTimeCompleted / completed, 1);
                var slaRate = assigned == 0 ? 0m : Math.Round(100m * notBreached / assigned, 1);
                var performanceScore = Math.Round(0.6m * slaRate + 0.4m * onTimeRate, 1);

                return new CompanyPerformanceItem(
                    g.Key,
                    companyNames.GetValueOrDefault(g.Key, "Unknown"),
                    assigned,
                    completed,
                    onTimeRate,
                    slaRate,
                    performanceScore);
            })
            .OrderByDescending(i => i.PerformanceScore)
            .ToList();

        logger.LogInformation("DEO company performance: {CompanyCount} companies", result.Count);
        return result;
    }
}
