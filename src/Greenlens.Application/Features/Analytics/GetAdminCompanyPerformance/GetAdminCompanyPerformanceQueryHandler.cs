using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    IDateTimeProvider clock,
    ILogger<GetAdminCompanyPerformanceQueryHandler> logger)
    : IRequestHandler<GetAdminCompanyPerformanceQuery, Result<List<CompanyPerformanceItem>>>
{
    public async Task<Result<List<CompanyPerformanceItem>>> Handle(
        GetAdminCompanyPerformanceQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting admin company performance");

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
                logger.LogInformation("Processing company: {CompanyId}", g.Key);

                var assigned = g.Count();
                logger.LogInformation("Assigned: {Assigned}", assigned);

                var completed = g.Count(r => r.Status is ReportStatus.Resolved or ReportStatus.Closed);
                logger.LogInformation("Completed: {Completed}", completed);

                var onTimeCompleted = g.Count(r =>
                    r.Status is ReportStatus.Resolved or ReportStatus.Closed
                    && r.ResolvedAt.HasValue
                    && (!r.SlaResolveDueAt.HasValue || r.ResolvedAt.Value <= r.SlaResolveDueAt.Value));
                logger.LogInformation("On time completed: {OnTimeCompleted}", onTimeCompleted);

                var notBreached = g.Count(r => !r.SlaResolveBreached);
                logger.LogInformation("Not breached: {NotBreached}", notBreached);

                var onTimeRate = completed == 0 ? 0m : Math.Round(100m * onTimeCompleted / completed, 1);
                logger.LogInformation("On time rate: {OnTimeRate}", onTimeRate);

                var slaRate = assigned == 0 ? 0m : Math.Round(100m * notBreached / assigned, 1);
                logger.LogInformation("SLA rate: {SlaRate}", slaRate);

                var performanceScore = Math.Round(0.6m * slaRate + 0.4m * onTimeRate, 1);
                logger.LogInformation("Performance score: {PerformanceScore}", performanceScore);

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

        logger.LogInformation("Admin company performance retrieved successfully");

        return result;
    }
}
