using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Map.GetMapViewportSummary;

/// <summary>Viewport summary for the home map card ("Khu vực đang xem").</summary>
public sealed record GetMapViewportSummaryQuery(
    decimal MinLat,
    decimal MaxLat,
    decimal MinLng,
    decimal MaxLng,
    int Days = PublicMapViewportSummaryLimits.DefaultDays,
    Guid? CategoryId = null) : IRequest<Result<MapViewportSummaryResponse>>;
