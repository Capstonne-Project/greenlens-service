using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetAdminGeographic;

public sealed record GetAdminGeographicQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<GeographicResponse>>;

public sealed record GeographicResponse(
    List<HeatmapPoint> Heatmap,
    List<GeographicMarker> Markers);

/// <summary>Grid cell (~0.01 deg) aggregating report density for the heatmap layer.</summary>
public sealed record HeatmapPoint(
    decimal Latitude,
    decimal Longitude,
    int Weight);

public sealed record GeographicMarker(
    Guid ReportId,
    decimal Latitude,
    decimal Longitude,
    ReportStatus Status);
