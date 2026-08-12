using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Analytics.GetAdminReportTrend;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Analytics.GetDeoReportTrend;

public sealed class GetDeoReportTrendQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<GetDeoReportTrendQueryHandler> logger)
    : IRequestHandler<GetDeoReportTrendQuery, Result<List<ReportTrendItem>>>
{
    private static readonly ReportStatus[] ResolvedStatuses =
        [ReportStatus.Resolved, ReportStatus.Closed];

    public async Task<Result<List<ReportTrendItem>>> Handle(
        GetDeoReportTrendQuery request, CancellationToken ct)
    {
        var scopeResult = await DepartmentContextResolver.ResolveAsync(users, currentUser, ct).ConfigureAwait(false);
        if (scopeResult.IsFailure)
            return scopeResult.Error!;

        var deptReports = DepartmentContextResolver.ApplyDepartmentScope(
            reports.QueryAsNoTracking(), scopeResult.Value.DepartmentId);

        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var created = await deptReports
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .Select(r => r.CreatedAt.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var resolved = await deptReports
            .Where(r => ResolvedStatuses.Contains(r.Status)
                        && r.ResolvedAt != null
                        && r.ResolvedAt >= from && r.ResolvedAt <= to)
            .Select(r => r.ResolvedAt!.Value.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var bucketedCreated = Bucket(created, request.GroupBy);
        var bucketedResolved = Bucket(resolved, request.GroupBy);
        var allKeys = bucketedCreated.Keys.Union(bucketedResolved.Keys).OrderBy(d => d);

        var result = allKeys
            .Select(date => new ReportTrendItem(
                DateOnly.FromDateTime(date),
                bucketedCreated.GetValueOrDefault(date),
                bucketedResolved.GetValueOrDefault(date)))
            .ToList();

        logger.LogInformation("DEO report trend: {PointCount} buckets", result.Count);
        return result;
    }

    private static Dictionary<DateTime, int> Bucket(List<DateTime> dates, ReportTrendGroupBy groupBy) =>
        dates.GroupBy(d => BucketKey(d, groupBy)).ToDictionary(g => g.Key, g => g.Count());

    private static DateTime BucketKey(DateTime date, ReportTrendGroupBy groupBy) => groupBy switch
    {
        ReportTrendGroupBy.Week => date.AddDays(-(int)date.DayOfWeek),
        ReportTrendGroupBy.Month => new DateTime(date.Year, date.Month, 1),
        _ => date
    };
}
