using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Analytics.GetCompanyWorkloadTrend;

/// <summary>Daily dispatched-vs-completed task trend, scoped to the caller's company.</summary>
public sealed class GetCompanyWorkloadTrendQueryHandler(
    IReportRepository reports,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    IDateTimeProvider clock)
    : IRequestHandler<GetCompanyWorkloadTrendQuery, Result<List<WorkloadTrendItem>>>
{
    private static readonly ReportStatus[] ResolvedStatuses =
        [ReportStatus.Resolved, ReportStatus.Closed];

    public async Task<Result<List<WorkloadTrendItem>>> Handle(
        GetCompanyWorkloadTrendQuery request, CancellationToken ct)
    {
        var companyIdResult = await CompanyContextResolver
            .ResolveCompanyIdAsync(companyStaff, currentUser.UserId, ct)
            .ConfigureAwait(false);
        if (companyIdResult.IsFailure)
            return companyIdResult.Error!;

        var companyId = companyIdResult.Value;
        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var assigned = await reports.QueryAsNoTracking()
            .Where(r => r.AssignedCompanyId == companyId
                        && r.DispatchedToCompanyAt >= from && r.DispatchedToCompanyAt <= to)
            .Select(r => r.DispatchedToCompanyAt!.Value.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var completed = await reports.QueryAsNoTracking()
            .Where(r => r.AssignedCompanyId == companyId
                        && ResolvedStatuses.Contains(r.Status)
                        && r.ResolvedAt != null
                        && r.ResolvedAt >= from && r.ResolvedAt <= to)
            .Select(r => r.ResolvedAt!.Value.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var assignedByDate = assigned.GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());
        var completedByDate = completed.GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());

        var allDates = assignedByDate.Keys.Union(completedByDate.Keys).OrderBy(d => d);

        var result = allDates
            .Select(date => new WorkloadTrendItem(
                DateOnly.FromDateTime(date),
                assignedByDate.GetValueOrDefault(date),
                completedByDate.GetValueOrDefault(date)))
            .ToList();

        return result;
    }
}
