using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Analytics.GetAdminGeographic;

/// <summary>
/// Heatmap grid (~0.01° cells) and recent markers for the admin geographic dashboard map.
/// Markers are capped at <see cref="MaxMarkers"/> most-recent reports to keep payload bounded.
/// </summary>
public sealed class GetAdminGeographicQueryHandler(
    IReportRepository reports,
    IDateTimeProvider clock,
    ILogger<GetAdminGeographicQueryHandler> logger)
    : IRequestHandler<GetAdminGeographicQuery, Result<GeographicResponse>>
{
    private const int MaxMarkers = 2000;
    private const decimal GridSize = 0.01m;

    public async Task<Result<GeographicResponse>> Handle(
        GetAdminGeographicQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting admin geographic");

        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var baseQuery = reports.QueryAsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to);

        logger.LogInformation("Base query: {BaseQuery}", baseQuery);

        var points = await baseQuery
            .Select(r => new { r.Latitude, r.Longitude })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Points: {Points}", points);

        var heatmap = points
            .GroupBy(p => (
                Lat: Math.Floor(p.Latitude / GridSize) * GridSize,
                Lng: Math.Floor(p.Longitude / GridSize) * GridSize))
            .Select(g => new HeatmapPoint(g.Key.Lat, g.Key.Lng, g.Count()))
            .ToList();

        logger.LogInformation("Heatmap: {Heatmap}", heatmap);

        var markers = await baseQuery
            .OrderByDescending(r => r.CreatedAt)
            .Take(MaxMarkers)
            .Select(r => new GeographicMarker(r.Id, r.Latitude, r.Longitude, r.Status))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Markers: {Markers}", markers);

        logger.LogInformation("Admin geographic retrieved successfully");

        return new GeographicResponse(heatmap, markers);
    }
}
