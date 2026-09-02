using Greenlens.Application.Common.CitizenMap;

namespace Greenlens.Application.Features.CitizenMap.GetCitizenMapWards;

/// <param name="GeoJson">GeoJSON geometry (Polygon/MultiPolygon) string from PostGIS; null if boundary not imported yet.</param>
/// <param name="ActiveReportCount">Reports currently unresolved (Verified/Reopened/InProgress) in this ward.</param>
/// <param name="Level">1 (None) – 5 (Critical), computed from <see cref="ActiveReportCount"/> by BE — FE only renders it.</param>
/// <param name="ColorHex">Display color matching <see cref="Level"/>, for FE map fill/legend.</param>
public sealed record CitizenMapWardDto(
    string Code,
    string Name,
    string? UnitAbbreviation,
    string? GeoJson,
    int ActiveReportCount,
    WardRiskLevel Level,
    string ColorHex);
