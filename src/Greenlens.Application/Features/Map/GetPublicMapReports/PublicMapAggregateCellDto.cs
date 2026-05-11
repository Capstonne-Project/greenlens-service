using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Map.GetPublicMapReports;

/// <summary>One grid cell summary when mode=aggregate (zoomed-out map).</summary>
public sealed record PublicMapAggregateCellDto(
    decimal CenterLatitude,
    decimal CenterLongitude,
    int Count,
    Severity MaxSeverity);
