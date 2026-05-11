using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Map;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Map.GetPublicMapReports;

/// <summary>
/// Returns pollution reports visible on the public map within a bounding box.
/// </summary>
/// <remarks>
/// Implements: BR-MAP-004 (round public coordinates ~11 m),
/// BR-REP-003 (viewport within Vietnam bounds via validator),
/// BR-MAP-012 (bounded queries — limit / max bbox / aggregate cap),
/// Public visibility: Verified, InProgress, Resolved, Closed only.
/// </remarks>
public sealed class GetPublicMapReportsQueryHandler(
    IReportRepository reports,
    IPollutionCategoryRepository categories)
    : IRequestHandler<GetPublicMapReportsQuery, Result<PublicMapReportsResponse>>
{
    private static readonly ReportStatus[] PublicStatuses =
    [
        ReportStatus.Verified,
        ReportStatus.InProgress,
        ReportStatus.Resolved,
        ReportStatus.Closed
    ];

    public async Task<Result<PublicMapReportsResponse>> Handle(
        GetPublicMapReportsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.CategoryId.HasValue)
        {
            var categoryOk = await categories.ExistsAsync(
                    c => c.Id == request.CategoryId.Value && c.IsActive,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!categoryOk)
                return Errors.Reports.CategoryNotFound;
        }

        var mode = request.Mode.Trim().ToLowerInvariant();
        var minLat = request.MinLat;
        var maxLat = request.MaxLat;
        var minLng = request.MinLng;
        var maxLng = request.MaxLng;

        var baseQuery = reports.QueryAsNoTracking()
            .Where(r => PublicStatuses.Contains(r.Status))
            .Where(r =>
                r.Latitude >= minLat &&
                r.Latitude <= maxLat &&
                r.Longitude >= minLng &&
                r.Longitude <= maxLng);

        if (request.CategoryId.HasValue)
            baseQuery = baseQuery.Where(r => r.CategoryId == request.CategoryId.Value);

        if (mode == "aggregate")
            return await HandleAggregateAsync(baseQuery, request, cancellationToken).ConfigureAwait(false);

        return await HandleDetailAsync(baseQuery, request, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Result<PublicMapReportsResponse>> HandleDetailAsync(
        IQueryable<Report> baseQuery,
        GetPublicMapReportsQuery request,
        CancellationToken cancellationToken)
    {
        var limit = request.Limit.HasValue
            ? Math.Clamp(request.Limit.Value, 1, PublicMapQueryLimits.MaxDetailLimit)
            : PublicMapQueryLimits.DefaultDetailLimit;

        var raw = await baseQuery
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.Latitude,
                r.Longitude,
                r.Severity,
                CategoryCode = r.Category!.Code,
                Title = r.Category!.NameVi,
                CategoryIconUrl = r.Category!.IconUrl,
                r.Description,
                r.Address,
                r.ReporterCount,
                ImageUrl = r.Media
                    .Where(m => m.Type == MediaType.Image)
                    .OrderBy(m => m.UploadedAt)
                    .Select(m => m.ThumbnailUrl ?? m.Url)
                    .FirstOrDefault(),
                r.Status,
                r.CreatedAt
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = raw
            .Select(x => new PublicMapReportPinDto(
                x.Id,
                x.Code,
                PublicMapCoordinateRounding.RoundLatitude(x.Latitude),
                PublicMapCoordinateRounding.RoundLongitude(x.Longitude),
                x.Severity,
                x.CategoryCode,
                x.Title,
                x.CategoryIconUrl,
                x.Description,
                x.Address,
                x.ReporterCount,
                x.ImageUrl,
                x.Status,
                x.CreatedAt))
            .ToList();

        var meta = new PublicMapReportsMetaDto(items.Count, limit, null, null);

        return new PublicMapReportsResponse("detail", items, null, meta);
    }

    private static async Task<Result<PublicMapReportsResponse>> HandleAggregateAsync(
        IQueryable<Report> baseQuery,
        GetPublicMapReportsQuery request,
        CancellationToken cancellationToken)
    {
        var gridLevel = request.GridLevel ?? PublicMapQueryLimits.DefaultGridLevel;
        gridLevel = Math.Clamp(
            gridLevel,
            PublicMapQueryLimits.MinGridLevel,
            PublicMapQueryLimits.MaxGridLevel);

        var cellSize = PublicMapQueryLimits.CellSizeDegrees(gridLevel);

        var points = await baseQuery
            .Select(r => new PointRow(r.Latitude, r.Longitude, r.Severity))
            .Take(PublicMapQueryLimits.MaxRowsForAggregateGrouping)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var cells = BuildAggregateCells(points, cellSize);
        var meta = new PublicMapReportsMetaDto(cells.Count, null, gridLevel, cellSize);

        return new PublicMapReportsResponse("aggregate", null, cells, meta);
    }

    private static List<PublicMapAggregateCellDto> BuildAggregateCells(
        List<PointRow> points,
        decimal cellSizeDeg)
    {
        var cell = (double)cellSizeDeg;
        var buckets = new Dictionary<(long Gi, long Gj), Accumulator>();

        foreach (var p in points)
        {
            var lat = (double)p.Latitude;
            var lng = (double)p.Longitude;
            var gi = (long)Math.Floor(lat / cell);
            var gj = (long)Math.Floor(lng / cell);
            var key = (gi, gj);

            if (!buckets.TryGetValue(key, out var acc))
            {
                buckets[key] = new Accumulator(1, p.Severity);
                continue;
            }

            buckets[key] = new Accumulator(
                acc.Count + 1,
                MaxSeverity(acc.MaxSeverity, p.Severity));
        }

        return buckets
            .Select(kv =>
            {
                var (gi, gj) = kv.Key;
                var acc = kv.Value;
                var centerLat = PublicMapCoordinateRounding.RoundLatitude((decimal)((gi + 0.5d) * cell));
                var centerLng = PublicMapCoordinateRounding.RoundLongitude((decimal)((gj + 0.5d) * cell));
                return new PublicMapAggregateCellDto(centerLat, centerLng, acc.Count, acc.MaxSeverity);
            })
            .OrderByDescending(c => c.Count)
            .ThenByDescending(c => SeverityRank(c.MaxSeverity))
            .ToList();
    }

    private static int SeverityRank(Severity s) => s switch
    {
        Severity.Critical => 4,
        Severity.High => 3,
        Severity.Medium => 2,
        _ => 1
    };

    private static Severity MaxSeverity(Severity a, Severity b) =>
        SeverityRank(a) >= SeverityRank(b) ? a : b;

    private sealed record PointRow(decimal Latitude, decimal Longitude, Severity Severity);

    private readonly record struct Accumulator(int Count, Severity MaxSeverity);
}
