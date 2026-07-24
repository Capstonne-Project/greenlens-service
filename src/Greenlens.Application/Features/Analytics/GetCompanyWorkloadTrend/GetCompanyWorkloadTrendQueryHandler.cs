using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Analytics.GetCompanyWorkloadTrend;

/// <summary>Daily dispatched-vs-completed task trend, scoped to the caller's company.</summary>
public sealed class GetCompanyWorkloadTrendQueryHandler(
    IReportRepository reports,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<GetCompanyWorkloadTrendQueryHandler> logger)
    : IRequestHandler<GetCompanyWorkloadTrendQuery, Result<List<WorkloadTrendItem>>>
{
    private static readonly ReportStatus[] ResolvedStatuses =
        [ReportStatus.Resolved, ReportStatus.Closed];

    public async Task<Result<List<WorkloadTrendItem>>> Handle(
        GetCompanyWorkloadTrendQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting company workload trend");

        var companyIdResult = await CompanyContextResolver
            .ResolveCompanyIdAsync(companyStaff, currentUser.UserId, ct)
            .ConfigureAwait(false);
        logger.LogInformation("Company ID: {CompanyId}", companyIdResult.Value);
        if (companyIdResult.IsFailure)
            {
                logger.LogError("Failed to resolve company ID: {Error}", companyIdResult.Error);
                return companyIdResult.Error!;
            }

        var companyId = companyIdResult.Value;
        logger.LogInformation("Company ID: {CompanyId}", companyId);
        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);
        logger.LogInformation("From: {From}, To: {To}", from, to);

        var assigned = await reports.QueryAsNoTracking()
            .Where(r => r.AssignedCompanyId == companyId
                        && r.DispatchedToCompanyAt >= from && r.DispatchedToCompanyAt <= to)
            .Select(r => r.DispatchedToCompanyAt!.Value.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Assigned: {Assigned}", assigned);

        var completed = await reports.QueryAsNoTracking()
            .Where(r => r.AssignedCompanyId == companyId
                        && ResolvedStatuses.Contains(r.Status)
                        && r.ResolvedAt != null
                        && r.ResolvedAt >= from && r.ResolvedAt <= to)
            .Select(r => r.ResolvedAt!.Value.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Completed: {Completed}", completed);

        var assignedByDate = assigned.GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());
        var completedByDate = completed.GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());

        logger.LogInformation("Assigned by date: {AssignedByDate}", assignedByDate);
        logger.LogInformation("Completed by date: {CompletedByDate}", completedByDate);

        var allDates = assignedByDate.Keys.Union(completedByDate.Keys).OrderBy(d => d);

        var result = allDates
            .Select(date => new WorkloadTrendItem(
                DateOnly.FromDateTime(date),
                assignedByDate.GetValueOrDefault(date),
                completedByDate.GetValueOrDefault(date)))
            .ToList();

        logger.LogInformation("Company workload trend retrieved successfully");

        return result;
    }
}
