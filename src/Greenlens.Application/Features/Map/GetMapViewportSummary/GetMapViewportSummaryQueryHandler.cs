using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Map;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Map.GetMapViewportSummary;

/// <summary>
/// Report count and daily trend for the map viewport card (home "Khu vực đang xem").
/// </summary>
/// <remarks>
/// Implements: BR-MAP-012 (bounded viewport query),
/// BR-REP-003 (Vietnam bbox via validator),
/// Public visibility: same statuses as GET /v1/map/reports (Verified+).
/// </remarks>
public sealed class GetMapViewportSummaryQueryHandler(
    IReportRepository reports,
    IPollutionCategoryRepository categories,
    IDateTimeProvider clock,
    ILogger<GetMapViewportSummaryQueryHandler> logger)
    : IRequestHandler<GetMapViewportSummaryQuery, Result<MapViewportSummaryResponse>>
{
    public async Task<Result<MapViewportSummaryResponse>> Handle(
        GetMapViewportSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (request.CategoryId.HasValue)
        {
            var categoryOk = await categories.ExistsAsync(
                    c => c.Id == request.CategoryId.Value && c.IsActive,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!categoryOk)
            {
                logger.LogWarning("Không tìm thấy danh mục với ID: {CategoryId}", request.CategoryId);
                return Errors.Reports.CategoryNotFound;
            }
        }

        var days = request.Days;
        var periodEnd = DateOnly.FromDateTime(clock.UtcNow);
        var periodStart = periodEnd.AddDays(-(days - 1));
        var sinceUtc = periodStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var baseQuery = reports.QueryAsNoTracking()
            .Where(r => PublicMapReportStatuses.Visible.Contains(r.Status))
            .Where(r => !r.IsHidden) // BR-ADM-006: hide moderated reports
            .Where(r =>
                r.Latitude >= request.MinLat &&
                r.Latitude <= request.MaxLat &&
                r.Longitude >= request.MinLng &&
                r.Longitude <= request.MaxLng)
            .Where(r => r.CreatedAt >= sinceUtc);

        if (request.CategoryId.HasValue)
            baseQuery = baseQuery.Where(r => r.CategoryId == request.CategoryId.Value);

        var reportCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var countsByDate = await baseQuery
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var lookup = countsByDate.ToDictionary(
            x => DateOnly.FromDateTime(x.Date),
            x => x.Count);

        var dailyCounts = new List<MapViewportDailyCountDto>(days);
        for (var d = periodStart; d <= periodEnd; d = d.AddDays(1))
        {
            lookup.TryGetValue(d, out var count);
            dailyCounts.Add(new MapViewportDailyCountDto(d, count));
        }

        logger.LogInformation(
            "Map viewport summary: {ReportCount} reports in bbox, period {Days} days",
            reportCount,
            days);

        return new MapViewportSummaryResponse(
            reportCount,
            days,
            periodStart,
            periodEnd,
            dailyCounts);
    }
}
