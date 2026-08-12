using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Analytics.GetAdminGeographic;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Analytics.GetDeoGeographic;

public sealed class GetDeoGeographicQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<GetDeoGeographicQueryHandler> logger)
    : IRequestHandler<GetDeoGeographicQuery, Result<GeographicResponse>>
{
    private const int MaxMarkers = 2000;
    private const decimal GridSize = 0.01m;

    public async Task<Result<GeographicResponse>> Handle(
        GetDeoGeographicQuery request, CancellationToken ct)
    {
        var scopeResult = await DepartmentContextResolver.ResolveAsync(users, currentUser, ct).ConfigureAwait(false);
        if (scopeResult.IsFailure)
            return scopeResult.Error!;

        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var baseQuery = DepartmentContextResolver
            .ApplyDepartmentScope(reports.QueryAsNoTracking(), scopeResult.Value.DepartmentId)
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to);

        var points = await baseQuery
            .Select(r => new { r.Latitude, r.Longitude })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var heatmap = points
            .GroupBy(p => (
                Lat: Math.Floor(p.Latitude / GridSize) * GridSize,
                Lng: Math.Floor(p.Longitude / GridSize) * GridSize))
            .Select(g => new HeatmapPoint(g.Key.Lat, g.Key.Lng, g.Count()))
            .ToList();

        var markers = await baseQuery
            .OrderByDescending(r => r.CreatedAt)
            .Take(MaxMarkers)
            .Select(r => new GeographicMarker(r.Id, r.Latitude, r.Longitude, r.Status))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("DEO geographic: {HeatmapCells} cells, {MarkerCount} markers", heatmap.Count, markers.Count);
        return new GeographicResponse(heatmap, markers);
    }
}
