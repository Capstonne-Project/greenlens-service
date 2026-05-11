using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Map.GetPublicMapReports;

/// <summary>Viewport query for verified-and-later reports (public map).</summary>
public sealed record GetPublicMapReportsQuery(
    decimal MinLat,
    decimal MaxLat,
    decimal MinLng,
    decimal MaxLng,
    string Mode = "detail",
    int? Limit = null,
    int? GridLevel = null,
    Guid? CategoryId = null) : IRequest<Result<PublicMapReportsResponse>>;
