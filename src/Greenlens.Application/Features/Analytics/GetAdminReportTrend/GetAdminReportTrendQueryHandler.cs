using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Analytics.GetAdminReportTrend;

/// <summary>Daily/weekly/monthly created-vs-resolved report trend for the admin dashboard chart.</summary>
public sealed class GetAdminReportTrendQueryHandler(
    IReportRepository reports,
    IDateTimeProvider clock,
    ILogger<GetAdminReportTrendQueryHandler> logger)
    : IRequestHandler<GetAdminReportTrendQuery, Result<List<ReportTrendItem>>>
{
    private static readonly ReportStatus[] ResolvedStatuses =
        [ReportStatus.Resolved, ReportStatus.Closed];

    public async Task<Result<List<ReportTrendItem>>> Handle(
        GetAdminReportTrendQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting admin report trend");

        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var created = await reports.QueryAsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .Select(r => r.CreatedAt.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Created: {Created}", created);

        var resolved = await reports.QueryAsNoTracking()
            .Where(r => ResolvedStatuses.Contains(r.Status)
                        && r.ResolvedAt != null
                        && r.ResolvedAt >= from && r.ResolvedAt <= to)
            .Select(r => r.ResolvedAt!.Value.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Resolved: {Resolved}", resolved);

        var bucketedCreated = Bucket(created, request.GroupBy);
        var bucketedResolved = Bucket(resolved, request.GroupBy);

        var allKeys = bucketedCreated.Keys.Union(bucketedResolved.Keys).OrderBy(d => d);

        var result = allKeys
            .Select(date => new ReportTrendItem(
                DateOnly.FromDateTime(date),
                bucketedCreated.GetValueOrDefault(date),
                bucketedResolved.GetValueOrDefault(date)))
            .ToList();

        logger.LogInformation("Admin report trend retrieved successfully");

        return result;
    }

    private static Dictionary<DateTime, int> Bucket(List<DateTime> dates, ReportTrendGroupBy groupBy)
    {
        return dates
            .GroupBy(d => BucketKey(d, groupBy))
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static DateTime BucketKey(DateTime date, ReportTrendGroupBy groupBy) => groupBy switch
    {
        ReportTrendGroupBy.Week => date.AddDays(-(int)date.DayOfWeek),
        ReportTrendGroupBy.Month => new DateTime(date.Year, date.Month, 1),
        _ => date
    };
}
