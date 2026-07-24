using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Analytics.GetAdminCompanyPerformance;

/// <summary>
/// KPIs per environmental service company: task volume, completion, on-time and SLA rates.
/// OnTimeRate: % of completed tasks resolved before their SLA due date.
/// SlaRate: % of all assigned tasks not flagged as SLA-breached (includes still-open overdue tasks).
/// PerformanceScore: 60% SlaRate + 40% OnTimeRate.
/// </summary>
public sealed class GetAdminCompanyPerformanceQueryHandler(
    IReportRepository reports,
    IEnvironmentalServiceCompanyRepository companies,
    IDateTimeProvider clock)
    : IRequestHandler<GetAdminCompanyPerformanceQuery, Result<List<CompanyPerformanceItem>>>
{
    public async Task<Result<List<CompanyPerformanceItem>>> Handle(
        GetAdminCompanyPerformanceQuery request, CancellationToken ct)
    {
        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var dispatched = await reports.QueryAsNoTracking()
            .Where(r => r.AssignedCompanyId != null
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

        return result;
    }
}
